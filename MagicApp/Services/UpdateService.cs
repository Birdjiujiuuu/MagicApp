using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.System;

namespace MagicApp.Services
{
    public class UpdateService
    {
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();
        private static readonly SemaphoreSlim _checkSemaphore = new SemaphoreSlim(1, 1);
        private static bool _isChecking = false;
        private static bool _isDialogShowing = false;
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static readonly TimeSpan CHECK_DEBOUNCE = TimeSpan.FromSeconds(2); // 2秒防抖

        /// <summary>
        /// 静默检查更新（只返回结果，不显示对话框）
        /// </summary>
        public static async Task<UpdateResult> CheckForUpdateSilentlyAsync()
        {
            // 防抖检查，避免频繁调用
            if (DateTime.Now - _lastCheckTime < CHECK_DEBOUNCE)
            {
                var packageVersion = Package.Current.Id.Version;
                string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
                return UpdateResult.CreateNoUpdate(currentVersion);
            }

            _lastCheckTime = DateTime.Now;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    // 获取当前应用版本
                    var packageVersion = Package.Current.Id.Version;
                    string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";

                    // 设置User-Agent头
                    string userAgentString = $"MagicApp/{currentVersion}";
                    httpClient.DefaultRequestHeaders.Add("User-Agent", userAgentString);

                    string url = "https://api.github.com/repos/Birdjiujiuuu/MagicApp/releases/latest";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();

                        using (var json = JsonDocument.Parse(jsonString))
                        {
                            string newestVersion = json.RootElement.GetProperty("tag_name").GetString() ?? string.Empty;
                            newestVersion = newestVersion.TrimStart('v', 'V');

                            if (newestVersion == currentVersion)
                            {
                                return UpdateResult.CreateNoUpdate(currentVersion);
                            }
                            else
                            {
                                string releaseTitle = json.RootElement.GetProperty("name").GetString() ?? string.Empty;
                                string releaseNotes = json.RootElement.GetProperty("body").GetString() ?? string.Empty;
                                string releaseUrl = json.RootElement.GetProperty("html_url").GetString() ?? string.Empty;
                                string downloadUrl = json.RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString() ?? string.Empty;

                                return UpdateResult.CreateUpdateAvailable(
                                    currentVersion,
                                    newestVersion,
                                    releaseTitle,
                                    releaseNotes,
                                    releaseUrl,
                                    downloadUrl
                                );
                            }
                        }
                    }
                    else
                    {
                        return UpdateResult.CreateError(
                            currentVersion,
                            $"{_resourceLoader.GetString("UpdateService_Error")}:\n(HTTP {response.StatusCode})"
                        );
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    var packageVersion = Package.Current.Id.Version;
                    string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
                    return UpdateResult.CreateError(
                        currentVersion,
                        $"{_resourceLoader.GetString("UpdateService_Error")}:\n{httpEx.Message}"
                    );
                }
                catch (Exception ex)
                {
                    var packageVersion = Package.Current.Id.Version;
                    string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
                    return UpdateResult.CreateError(currentVersion, $"{_resourceLoader.GetString("UpdateService_Error")}:\n{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 检查更新并显示对话框
        /// </summary>
        public static async Task CheckForUpdateAsync(XamlRoot xamlRoot, Action? onCheckStart = null, Action? onCheckEnd = null)
        {
            // 等待信号量，确保只有一个检查在执行
            await _checkSemaphore.WaitAsync();

            try
            {
                _isChecking = true;
                onCheckStart?.Invoke();

                var result = await CheckForUpdateSilentlyAsync();

                // 如果已经有对话框在显示，则跳过
                if (_isDialogShowing)
                {
                    onCheckEnd?.Invoke();
                    return;
                }

                if (!result.IsSuccess)
                {
                    _isDialogShowing = true;
                    await ShowErrorDialogAsync(xamlRoot, result.ErrorMessage ?? _resourceLoader.GetString("UpdateService_Error"));
                    _isDialogShowing = false;
                }
                else if (!result.HasUpdate)
                {
                    _isDialogShowing = true;
                    await ShowLatestDialogAsync(xamlRoot);
                    _isDialogShowing = false;
                }
                else
                {
                    _isDialogShowing = true;
                    await ShowUpdateAvailableDialogAsync(xamlRoot, result.ReleaseTitle ?? "", result.ReleaseNotes ?? "", result.ReleaseUrl ?? "", result.DownloadUrl ?? "", result.NewVersion ?? "");
                    _isDialogShowing = false;
                }
            }
            finally
            {
                _isChecking = false;
                onCheckEnd?.Invoke();
                _checkSemaphore.Release();
            }
        }

        /// <summary>
        /// 静默检查并在有更新时显示对话框
        /// </summary>
        public static async Task CheckForUpdateSilentlyAndNotifyAsync(XamlRoot xamlRoot)
        {
            // 如果已经有检查在进行，则跳过
            if (_isChecking)
                return;

            // 等待信号量，确保只有一个检查在执行
            await _checkSemaphore.WaitAsync();

            try
            {
                _isChecking = true;
                var result = await CheckForUpdateSilentlyAsync();

                // 只在有更新且成功检查且没有对话框在显示时显示对话框
                if (result.IsSuccess && result.HasUpdate && !_isDialogShowing)
                {
                    _isDialogShowing = true;
                    await ShowUpdateAvailableDialogAsync(xamlRoot, result.ReleaseTitle ?? "", result.ReleaseNotes ?? "", result.ReleaseUrl ?? "", result.DownloadUrl ?? "", result.NewVersion ?? "");
                    _isDialogShowing = false;
                }
                // 注意：不显示无更新和错误对话框，保持静默
            }
            finally
            {
                _isChecking = false;
                _checkSemaphore.Release();
            }
        }

        /// <summary>
        /// 显示已是最新版本的对话框
        /// </summary>
        private static async Task ShowLatestDialogAsync(XamlRoot xamlRoot)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = _resourceLoader.GetString("UpdateService_Title"),
                Content = _resourceLoader.GetString("UpdateService_Latest"),
                CloseButtonText = _resourceLoader.GetString("UpdateService_Close"),
                DefaultButton = ContentDialogButton.Close
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// 显示有可用更新的对话框
        /// </summary>
        private static async Task ShowUpdateAvailableDialogAsync(XamlRoot xamlRoot, string title, string releaseNotes, string releaseUrl, string downloadUrl, string newestVersion)
        {
            // 创建WebView2控件
            var webView = new Microsoft.UI.Xaml.Controls.WebView2();
            webView.DefaultBackgroundColor = Colors.Transparent;
            webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView.VerticalAlignment = VerticalAlignment.Stretch;

            // 创建容器并添加WebView2
            var container = new Grid();

            // 设置容器的尺寸以填充对话框内容区域
            container.MinHeight = 200;
            container.MinWidth = 400;
            container.HorizontalAlignment = HorizontalAlignment.Stretch;
            container.VerticalAlignment = VerticalAlignment.Stretch;

            // 确保WebView2在容器中填满整个空间
            container.Children.Add(webView);

            // 确保WebView2已初始化
            await webView.EnsureCoreWebView2Async();

            // 创建Markdown HTML页面
            string htmlContent = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <style>
            html {{
                margin: 0;
                padding: 0;
                height: 100%;
                width: 100%;
                overflow: hidden;
            }}

            body {{
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                margin: 0;
                padding: 16px;
                background-color: {(App.AppTheme == ElementTheme.Dark ? "#2B2B2B" : "#ffffff")};
                color: {(App.AppTheme == ElementTheme.Dark ? "#ffffff" : "#000000")};
                line-height: 1.6;
                font-size: 14px;
                -webkit-font-smoothing: antialiased;
                -moz-osx-font-smoothing: grayscale;
                height: 100%;
                width: 100%;
                box-sizing: border-box;
                overflow-y: auto;
                overflow-x: hidden;
            }}
            h1, h2, h3, h4 {{
                margin-top: 20px;
                margin-bottom: 10px;
                color: {(App.AppTheme == ElementTheme.Dark ? "#ffffff" : "#000000")};
            }}
            h1 {{ font-size: 20px; }}
            h2 {{ font-size: 18px; }}
            h3 {{ font-size: 16px; }}
            h4 {{ font-size: 14px; }}
            
            a {{
                color: #0078d4;
                text-decoration: none;
            }}
            a:hover {{ text-decoration: underline; }}
            
            code {{
                background-color: {(App.AppTheme == ElementTheme.Dark ? "#2d2d30" : "#f5f5f5")};
                padding: 2px 4px;
                border-radius: 3px;
                font-family: 'Cascadia Mono', Consolas, 'Courier New', monospace;
                font-size: 12px;
            }}
            
            pre {{
                background-color: {(App.AppTheme == ElementTheme.Dark ? "#2d2d30" : "#f5f5f5")};
                padding: 12px;
                border-radius: 5px;
                overflow-x: auto;
                border: 1px solid {(App.AppTheme == ElementTheme.Dark ? "#3d3d40" : "#e1e1e1")};
            }}
            
            pre code {{
                background-color: transparent;
                padding: 0;
                font-size: 12px;
            }}
            
            blockquote {{
                border-left: 4px solid #0078d4;
                margin: 10px 0;
                padding: 8px 12px;
                background-color: {(App.AppTheme == ElementTheme.Dark ? "rgba(0, 120, 212, 0.1)" : "rgba(0, 120, 212, 0.05)")};
                color: {(App.AppTheme == ElementTheme.Dark ? "#cccccc" : "#333333")};
            }}
            
            ul, ol {{
                margin: 8px 0;
                padding-left: 24px;
            }}
            
            li {{
                margin: 4px 0;
            }}
            
            table {{
                border-collapse: collapse;
                width: 100%;
                margin: 12px 0;
            }}
            
            th, td {{
                border: 1px solid {(App.AppTheme == ElementTheme.Dark ? "#3d3d40" : "#e1e1e1")};
                padding: 8px 12px;
                text-align: left;
            }}
            
            th {{
                background-color: {(App.AppTheme == ElementTheme.Dark ? "#2d2d30" : "#f5f5f5")};
                font-weight: 600;
            }}
            
            hr {{
                border: none;
                border-top: 1px solid {(App.AppTheme == ElementTheme.Dark ? "#3d3d40" : "#e1e1e1")};
                margin: 20px 0;
            }}
            
            img {{
                max-width: 100%;
                height: auto;
            }}
            
            .release-header {{
                margin-bottom: 15px;
                border-bottom: 1px solid {(App.AppTheme == ElementTheme.Dark ? "#3d3d40" : "#e1e1e1")};
                padding-bottom: 10px;
                font-size: 24px;
            }}
        </style>
    </head>
    <body>
        <div class='release-header'>
            <span style='font-weight: 600;'>{EscapeHtml(title)}</span>
        </div>
        {ConvertMarkdownToHtml(releaseNotes)}
    </body>
    </html>";

            // 加载HTML内容
            webView.CoreWebView2.NavigateToString(htmlContent);

            // 创建对话框
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Content = container,
                PrimaryButtonText = _resourceLoader.GetString("UpdateService_Download"),
                SecondaryButtonText = _resourceLoader.GetString("UpdateService_Release"),
                CloseButtonText = _resourceLoader.GetString("UpdateService_Later"),
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 下载更新文件
                await DownloadFileAsync(downloadUrl, newestVersion, xamlRoot);
            }
            if (result == ContentDialogResult.Secondary)
            {
                // 打开发布页面
                await Launcher.LaunchUriAsync(new Uri(releaseUrl));
            }
        }

        /// <summary>
        /// 将Markdown转换为HTML
        /// </summary>
        private static string ConvertMarkdownToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "<p>" + _resourceLoader.GetString("UpdateService_NoReleaseNotes") + "</p>";
            }

            // 简单的Markdown转换
            string html = markdown
                // 转义HTML特殊字符
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                // 处理换行符
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                // 段落
                .Replace("\n\n", "</p><p>")
                .Replace("\n", "<br>");

            // 处理标题
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^###\s+(.+?)$", "<h3>$1</h3>", System.Text.RegularExpressions.RegexOptions.Multiline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^##\s+(.+?)$", "<h2>$1</h2>", System.Text.RegularExpressions.RegexOptions.Multiline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^#\s+(.+?)$", "<h1>$1</h1>", System.Text.RegularExpressions.RegexOptions.Multiline);

            // 处理列表项
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^-\s+(.+?)$", "<li>$1</li>", System.Text.RegularExpressions.RegexOptions.Multiline);
            html = System.Text.RegularExpressions.Regex.Replace(html, @"^\*\s+(.+?)$", "<li>$1</li>", System.Text.RegularExpressions.RegexOptions.Multiline);

            // 处理代码块（简单处理）
            html = System.Text.RegularExpressions.Regex.Replace(html, @"`(.+?)`", "<code>$1</code>");

            // 处理链接
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\[(.+?)\]\((.+?)\)", "<a href=\"$2\">$1</a>");

            // 处理加粗
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"__(.+?)__", "<strong>$1</strong>");

            // 处理斜体
            html = System.Text.RegularExpressions.Regex.Replace(html, @"\*(.+?)\*", "<em>$1</em>");
            html = System.Text.RegularExpressions.Regex.Replace(html, @"_(.+?)_", "<em>$1</em>");

            // 将<li>包装在<ul>中
            html = System.Text.RegularExpressions.Regex.Replace(html, @"(<li>.+?</li>)(?:\n|$)", "<ul>$1</ul>\n", System.Text.RegularExpressions.RegexOptions.Singleline);

            // 确保有<p>标签包裹
            if (!html.StartsWith("<h1>") && !html.StartsWith("<h2>") && !html.StartsWith("<h3>"))
            {
                html = "<p>" + html + "</p>";
            }

            return html;
        }

        /// <summary>
        /// 转义HTML特殊字符
        /// </summary>
        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        /// <summary>
        /// 下载更新文件
        /// </summary>
        private static async Task DownloadFileAsync(string downloadUrl, string newestVersion, XamlRoot xamlRoot)
        {
            try
            {
                var fileTypeChoices = new Dictionary<string, List<string>>
                {
                    { "ZIP", new List<string> { ".zip"} },
                    { _resourceLoader.GetString("FileDownloadService_AllFile"), new List<string> { "." } }
                };

                // 调用FileDownloadService
                bool success = await FileDownloadService.DownloadFileAsync(
                    downloadUrl,
                    "MagicApp_" + newestVersion,
                    ".zip",
                    fileTypeChoices,
                    xamlRoot);
            }
            finally
            {

            }
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private static async Task ShowErrorDialogAsync(XamlRoot xamlRoot, string errorMessage)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = _resourceLoader.GetString("UpdateService_Title"),
                Content = errorMessage,
                CloseButtonText = _resourceLoader.GetString("UpdateService_Close"),
                DefaultButton = ContentDialogButton.Close
            };
            await dialog.ShowAsync();
        }
    }

    /// <summary>
    /// 更新检查结果
    /// </summary>
    public class UpdateResult
    {
        /// <summary>
        /// 是否有更新可用
        /// </summary>
        public bool HasUpdate { get; set; }

        /// <summary>
        /// 检查是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 新版本号
        /// </summary>
        public string? NewVersion { get; set; }

        /// <summary>
        /// 当前版本号
        /// </summary>
        public string? CurrentVersion { get; set; }

        /// <summary>
        /// 更新标题
        /// </summary>
        public string? ReleaseTitle { get; set; }

        /// <summary>
        /// 更新说明
        /// </summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// 发行链接
        /// </summary>
        public string? ReleaseUrl { get; set; }

        /// <summary>
        /// 下载链接
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 创建无更新的结果
        /// </summary>
        public static UpdateResult CreateNoUpdate(string currentVersion)
        {
            return new UpdateResult
            {
                HasUpdate = false,
                IsSuccess = true,
                CurrentVersion = currentVersion
            };
        }

        /// <summary>
        /// 创建有更新的结果
        /// </summary>
        public static UpdateResult CreateUpdateAvailable(string currentVersion, string newVersion, string releaseTitle, string releaseNotes, string releaseUrl, string downloadUrl)
        {
            return new UpdateResult
            {
                HasUpdate = true,
                IsSuccess = true,
                CurrentVersion = currentVersion,
                NewVersion = newVersion,
                ReleaseTitle = releaseTitle,
                ReleaseNotes = releaseNotes,
                ReleaseUrl = releaseUrl,
                DownloadUrl = downloadUrl
            };
        }

        /// <summary>
        /// 创建错误结果
        /// </summary>
        public static UpdateResult CreateError(string currentVersion, string errorMessage)
        {
            return new UpdateResult
            {
                HasUpdate = false,
                IsSuccess = false,
                CurrentVersion = currentVersion,
                ErrorMessage = errorMessage
            };
        }
    }
}