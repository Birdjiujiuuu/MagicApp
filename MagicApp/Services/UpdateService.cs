using Microsoft.UI;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
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

        // 静默检查更新
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
                            $"{_resourceLoader.GetString("Services_Update_Error")}:\n(HTTP {response.StatusCode})"
                        );
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    var packageVersion = Package.Current.Id.Version;
                    string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
                    return UpdateResult.CreateError(
                        currentVersion,
                        $"{_resourceLoader.GetString("Services_Update_Error")}:\n{httpEx.Message}"
                    );
                }
                catch (Exception ex)
                {
                    var packageVersion = Package.Current.Id.Version;
                    string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
                    return UpdateResult.CreateError(currentVersion, $"{_resourceLoader.GetString("Services_Update_Error")}:\n{ex.Message}");
                }
            }
        }

        // 检查更新并显示对话框
        public static async Task CheckForUpdateAsync(XamlRoot xamlRoot, Action? onCheckStart = null, Action? onCheckEnd = null)
        {
            // 商店版本跳过更新检查
            if (Package.Current.SignatureKind == PackageSignatureKind.Store)
                return;

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
                    await ShowErrorDialogAsync(xamlRoot, result.ErrorMessage ?? _resourceLoader.GetString("Services_Update_Error"));
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

        // 静默检查并在有更新时显示对话框
        public static async Task CheckForUpdateSilentlyAndNotifyAsync(XamlRoot xamlRoot)
        {
            // 商店版本跳过更新检查
            if (Package.Current.SignatureKind == PackageSignatureKind.Store)
                return;

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

        // 显示已是最新版本的对话框
        private static async Task ShowLatestDialogAsync(XamlRoot xamlRoot)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = _resourceLoader.GetString("Services_Update_Title"),
                Content = _resourceLoader.GetString("Services_Update_Latest"),
                CloseButtonText = _resourceLoader.GetString("Services_Update_Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = App.AppTheme
            };
            await dialog.ShowAsync();
        }

        // 显示有可用更新的对话框
        private static async Task ShowUpdateAvailableDialogAsync(XamlRoot xamlRoot, string title, string releaseNotes, string releaseUrl, string downloadUrl, string newestVersion)
        {
            // 创建 WebView2 控件
            var webView = new Microsoft.UI.Xaml.Controls.WebView2
            {
                DefaultBackgroundColor = Colors.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // 创建容器并添加 WebView2
            var container = new Grid            
            {
                // 设置容器的尺寸以填充对话框内容区域
                MinHeight = 250,
                MinWidth = 500,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // 确保 WebView2 在容器中填满整个空间
            container.Children.Add(webView);

            // 确保 WebView2 已初始化
            await webView.EnsureCoreWebView2Async();

            // 使用 MarkdownRenderer 加载 Markdown
            MarkdownRenderer.LoadMarkdown(webView, releaseNotes, title, App.AppTheme);

            // 创建对话框
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Content = container,
                PrimaryButtonText = _resourceLoader.GetString("Services_Update_Download"),
                SecondaryButtonText = _resourceLoader.GetString("Services_Update_Release"),
                CloseButtonText = _resourceLoader.GetString("Services_Update_Later"),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = App.AppTheme
            };

            // 显示通知
            AppNotification notification = new AppNotificationBuilder()
                .AddText(_resourceLoader.GetString("Services_Update_NewVersionFound"))
                .AddText(title)
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 下载更新文件
                await DownloadFileAsync(downloadUrl, newestVersion, xamlRoot);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // 打开发布页面
                await Launcher.LaunchUriAsync(new Uri(releaseUrl));
            }
        }

        // 下载更新文件
        private static async Task DownloadFileAsync(string downloadUrl, string newestVersion, XamlRoot xamlRoot)
        {
            try
            {
                var fileTypeChoices = new Dictionary<string, List<string>>
                {
                    { "ZIP", new List<string> { ".zip"} },
                    { _resourceLoader.GetString("Services_FileDownload_AllFile"), new List<string> { "." } }
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

        // 显示错误对话框
        private static async Task ShowErrorDialogAsync(XamlRoot xamlRoot, string errorMessage)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = _resourceLoader.GetString("Services_Update_Title"),
                Content = errorMessage,
                CloseButtonText = _resourceLoader.GetString("Services_Update_Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = App.AppTheme
            };
            await dialog.ShowAsync();
        }
    }

    // 更新检查结果
    public class UpdateResult
    {
        // 是否有更新可用
        public bool HasUpdate { get; set; }

        // 检查是否成功
        public bool IsSuccess { get; set; }

        // 新版本号
        public string? NewVersion { get; set; }

        // 当前版本号
        public string? CurrentVersion { get; set; }

        // 更新标题
        public string? ReleaseTitle { get; set; }

        // 更新说明
        public string? ReleaseNotes { get; set; }

        // 发行链接
        public string? ReleaseUrl { get; set; }

        // 下载链接
        public string? DownloadUrl { get; set; }

        // 错误信息
        public string? ErrorMessage { get; set; }

        // 创建无更新的结果
        public static UpdateResult CreateNoUpdate(string currentVersion)
        {
            return new UpdateResult
            {
                HasUpdate = false,
                IsSuccess = true,
                CurrentVersion = currentVersion
            };
        }

        // 创建有更新的结果
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

        // 创建错误结果
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