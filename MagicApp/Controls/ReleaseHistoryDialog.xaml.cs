using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MagicApp.Controls
{
    public sealed partial class ReleaseHistoryDialog : UserControl
    {
        private static readonly HttpClient _http = new();

        private const double CardWidth = 620;
        private const double CardHeightPref = 620;
        private const double CloseGap = 10;
        private const double CloseSize = 40;

        // 缓存 markdown，主题切换时重渲
        private string _cachedMarkdown = "";

        // 外部注入
        public Popup? HostPopup { get; set; }
        public string RepoOwner { get; set; } = "Birdjiujiuuu";
        public string RepoName { get; set; } = "MagicApp";

        public double PreferredWidth => Card.Width + CloseGap + CloseSize;
        public double PreferredHeight => Card.Height;

        public ReleaseHistoryDialog()
        {
            InitializeComponent();

            if (!_http.DefaultRequestHeaders.UserAgent.TryParseAdd("MagicApp"))
            {
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("MagicApp");
            }
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            // 跟随应用主题
            RequestedTheme = App.AppTheme;
            ApplyTheme();

            Loaded += ReleaseHistoryDialog_Loaded;
            Unloaded += ReleaseHistoryDialog_Unloaded;
        }

        // ---------- 生命周期 ----------

        private void ReleaseHistoryDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (XamlRoot == null) return;

            ApplyLayoutForXamlRoot();

            XamlRoot.Changed -= XamlRoot_Changed;
            XamlRoot.Changed += XamlRoot_Changed;

            App.ThemeChanged -= App_ThemeChanged;
            App.ThemeChanged += App_ThemeChanged;
        }

        private void ReleaseHistoryDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            if (XamlRoot != null)
                XamlRoot.Changed -= XamlRoot_Changed;

            App.ThemeChanged -= App_ThemeChanged;
        }

        private void App_ThemeChanged(ElementTheme theme)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                RequestedTheme = theme;
                ApplyTheme();

                if (!string.IsNullOrEmpty(_cachedMarkdown))
                {
                    _ = MarkdownRenderer.LoadMarkdownAsync(
                        ContentWebView,
                        _cachedMarkdown,
                        "Release History",
                        ResolveEffectiveTheme());
                }
            });
        }

        private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            ApplyLayoutForXamlRoot();
        }

        private void ApplyLayoutForXamlRoot()
        {
            if (XamlRoot == null) return;

            double rootW = XamlRoot.Size.Width;
            double rootH = XamlRoot.Size.Height;

            RootOverlay.Width = rootW;
            RootOverlay.Height = rootH;

            Card.Width = Math.Min(CardWidth, Math.Max(400, rootW - 160));
            Card.Height = Math.Min(CardHeightPref, Math.Max(360, rootH - 120));
        }

        private ElementTheme ResolveEffectiveTheme()
        {
            if (RequestedTheme == ElementTheme.Default)
            {
                return Application.Current.RequestedTheme == ApplicationTheme.Dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light;
            }
            return RequestedTheme;
        }

        private void ApplyTheme()
        {
            bool isDark = ResolveEffectiveTheme() == ElementTheme.Dark;

            var bgColor = isDark
                ? Windows.UI.Color.FromArgb(0xFF, 0x2B, 0x2B, 0x2B)
                : Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            var bgBrush = new SolidColorBrush(bgColor);

            Card.Background = bgBrush;
            TitleRegion.Background = bgBrush;
            CloseButton.Background = bgBrush;
            ContentWebView.DefaultBackgroundColor = bgColor;
        }

        public void Dispose()
        {
            try { ContentWebView?.Close(); } catch { }

            if (XamlRoot != null)
                XamlRoot.Changed -= XamlRoot_Changed;

            App.ThemeChanged -= App_ThemeChanged;
            HostPopup = null;
        }

        // ---------- 数据加载 ----------

        public async Task LoadReleasesAsync()
        {
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            ContentWebView.Visibility = Visibility.Collapsed;

            try
            {
                string url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases?per_page=30";
                string json = await _http.GetStringAsync(url);

                _cachedMarkdown = ConvertReleasesToMarkdown(json);

                await MarkdownRenderer.LoadMarkdownAsync(
                    ContentWebView,
                    _cachedMarkdown,
                    "Release History",
                    ResolveEffectiveTheme());

                ContentWebView.Visibility = Visibility.Visible;
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
                ErrorText.Text = ex.Message;
                ErrorPanel.Visibility = Visibility.Visible;
            }
        }

        private static string ConvertReleasesToMarkdown(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                return "> No release notes available.";

            var sb = new StringBuilder();

            foreach (var r in root.EnumerateArray())
            {
                if (r.TryGetProperty("draft", out var d) && d.GetBoolean())
                    continue;

                bool prerelease = r.TryGetProperty("prerelease", out var pre) && pre.GetBoolean();

                string title = "";
                if (r.TryGetProperty("name", out var n))
                    title = n.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(title) &&
                    r.TryGetProperty("tag_name", out var tag))
                    title = tag.GetString() ?? "Release";

                string dateText = "";
                if (r.TryGetProperty("published_at", out var pub) &&
                    DateTimeOffset.TryParse(pub.GetString(), out var dt))
                {
                    dateText = dt.ToLocalTime().ToString("yyyy-MM-dd");
                }

                string suffix = prerelease ? " `Pre-release`" : "";

                sb.Append("# ").Append(title).AppendLine(suffix);
                if (!string.IsNullOrEmpty(dateText))
                    sb.Append('*').Append(dateText).AppendLine("*");
                sb.AppendLine();

                string body = "";
                if (r.TryGetProperty("body", out var b))
                    body = b.GetString() ?? "";

                sb.AppendLine(string.IsNullOrWhiteSpace(body) ? "_No description for this release._" : body);
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ---------- 关闭 / 重试 ----------

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (HostPopup != null)
                HostPopup.IsOpen = false;
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadReleasesAsync();
        }
    }
}