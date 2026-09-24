using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
        private const int FadeMs = 180;

        private string _cachedMarkdown = "";

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

            RequestedTheme = App.AppTheme;
            ApplyTheme();

            RootOverlay.Opacity = 0;
            Loaded += ReleaseHistoryDialog_Loaded;
        }

        // ---------- 生命周期 ----------

        private void ReleaseHistoryDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (XamlRoot == null) return;

            ApplyLayoutForXamlRoot();
            XamlRoot.Changed += XamlRoot_Changed;
            App.ThemeChanged += App_ThemeChanged;

            FadeRoot(1);
        }

        public void Dispose()
        {
            try { ContentWebView?.Close(); } catch { }

            if (XamlRoot != null)
                XamlRoot.Changed -= XamlRoot_Changed;

            App.ThemeChanged -= App_ThemeChanged;
            HostPopup = null;
        }

        private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            ApplyLayoutForXamlRoot();
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
                        ContentWebView, _cachedMarkdown, "Release History", EffectiveTheme);
                }
            });
        }

        // ---------- 布局 / 主题 ----------

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

        private ElementTheme EffectiveTheme =>
            RequestedTheme != ElementTheme.Default
                ? RequestedTheme
                : Application.Current.RequestedTheme == ApplicationTheme.Dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light;

        private void ApplyTheme()
        {
            var bgColor = EffectiveTheme == ElementTheme.Dark
                ? Windows.UI.Color.FromArgb(0xFF, 0x2B, 0x2B, 0x2B)
                : Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            var brush = new SolidColorBrush(bgColor);

            Card.Background = brush;
            TitleRegion.Background = brush;
            CloseButton.Background = brush;
            ContentWebView.DefaultBackgroundColor = bgColor;
        }

        // ---------- 动画 ----------

        private void FadeRoot(double to, Action? onCompleted = null)
        {
            var sb = new Storyboard();

            var anim = new DoubleAnimation
            {
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(FadeMs)),
                EasingFunction = new CubicEase
                {
                    EasingMode = to > 0 ? EasingMode.EaseOut : EasingMode.EaseIn
                }
            };

            Storyboard.SetTarget(anim, RootOverlay);
            Storyboard.SetTargetProperty(anim, "Opacity");
            sb.Children.Add(anim);

            if (onCompleted != null)
                sb.Completed += (_, _) => onCompleted();

            sb.Begin();
        }

        // ---------- 状态 ----------

        private void SetState(bool loading, string? error = null)
        {
            LoadingRing.IsActive = loading;
            LoadingRing.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
            ErrorPanel.Visibility = error != null ? Visibility.Visible : Visibility.Collapsed;
            ContentWebView.Visibility = !loading && error == null ? Visibility.Visible : Visibility.Collapsed;

            if (error != null) ErrorText.Text = error;
        }

        // ---------- 数据加载 ----------

        public async Task LoadReleasesAsync()
        {
            SetState(loading: true);

            try
            {
                var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases?per_page=30";
                var json = await _http.GetStringAsync(url);
                _cachedMarkdown = ToMarkdown(json);

                await MarkdownRenderer.LoadMarkdownAsync(
                    ContentWebView, _cachedMarkdown, "Release History", EffectiveTheme);

                SetState(loading: false);
            }
            catch (Exception ex)
            {
                SetState(loading: false, error: ex.Message);
            }
        }

        private static string ToMarkdown(string json)
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

                string title = r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(title) &&
                    r.TryGetProperty("tag_name", out var tag))
                    title = tag.GetString() ?? "Release";

                string dateText = "";
                if (r.TryGetProperty("published_at", out var pub) &&
                    DateTimeOffset.TryParse(pub.GetString(), out var dt))
                {
                    dateText = dt.ToLocalTime().ToString("yyyy-MM-dd");
                }

                string suffix = string.IsNullOrEmpty(dateText) ? "" : $" `{dateText}`";
                sb.Append("# ").Append(title).AppendLine(suffix).AppendLine();

                string body = r.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
                sb.AppendLine(string.IsNullOrWhiteSpace(body) ? "_No description for this release._" : body);
                sb.AppendLine().AppendLine("---").AppendLine();
            }

            return sb.ToString();
        }

        // ---------- 关闭 / 重试 ----------

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseButton.IsEnabled = false;
            FadeRoot(0, () =>
            {
                if (HostPopup != null) HostPopup.IsOpen = false;
            });
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadReleasesAsync();
        }
    }
}