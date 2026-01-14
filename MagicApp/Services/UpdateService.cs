using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            ContentDialog dialog = new()
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = releaseNotes,
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
        /// 下载更新文件
        /// </summary>
        private static async Task DownloadFileAsync(string downloadUrl, string newestVersion, XamlRoot xamlRoot)
        {
            try
            {
                var fileTypeChoices = new Dictionary<string, List<string>>
                {
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