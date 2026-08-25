using MagicApp.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MagicApp.Services
{
    /// <summary>
    /// 提供文件下载功能，支持进度显示、取消下载、文件类型选择等。
    /// </summary>
    public static class FileDownloadService
    {
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        /// <summary>
        /// 通用的文件下载方法（所有便捷方法最终调用此方法）
        /// </summary>
        public static async Task<bool> DownloadFileAsync(
            string downloadUrl,
            string suggestedFileName,
            string defaultExtension,
            Dictionary<string, List<string>>? fileTypeChoices,
            XamlRoot xamlRoot,
            string? dialogTitle = null,
            string? successMessage = null,
            string? failureMessage = null)
        {
            // 参数校验与默认值
            if (string.IsNullOrEmpty(downloadUrl))
                throw new ArgumentException(_resourceLoader.GetString("Services_FileDownload_UrlNoNull"));

            if (string.IsNullOrEmpty(suggestedFileName))
                suggestedFileName = $"Download_{DateTime.Now:yyyyMMdd_HHmmss}";

            if (!string.IsNullOrEmpty(defaultExtension) && !defaultExtension.StartsWith("."))
                defaultExtension = "." + defaultExtension;

            // 确保默认扩展名包含在文件类型列表中
            EnsureDefaultExtensionInChoices(ref fileTypeChoices, defaultExtension);

            try
            {
                // 1. 让用户选择保存位置
                var targetFile = await PickSaveFileAsync(suggestedFileName, defaultExtension, fileTypeChoices);
                if (targetFile == null)
                    return false; // 用户取消了选择

                // 2. 准备进度对话框和取消令牌
                var (progressDialog, cancellationTokenSource) = CreateProgressDialog(xamlRoot, dialogTitle);
                var cancellationToken = cancellationTokenSource.Token;

                // 3. 开始下载并显示对话框
                var dialogTask = progressDialog.ShowAsync();

                try
                {
                    // 执行实际下载
                    await DownloadStreamToFileAsync(
                        downloadUrl,
                        targetFile,
                        cancellationToken,
                        (downloaded, total, speed, remaining) =>
                            UpdateProgressUI(progressDialog, downloaded, total, speed, remaining)
                    );

                    // 下载成功 → 更新对话框为“成功”并等待用户关闭
                    CompleteWithSuccess(progressDialog, successMessage, targetFile.Path);
                    await dialogTask;
                    return true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // 用户取消 → 删除未完成的文件，关闭对话框
                    await DeleteFilePermanentlyAsync(targetFile);
                    progressDialog.Hide();
                    await dialogTask; // 确保对话框完全关闭
                    return false;
                }
                catch (Exception ex)
                {
                    // 下载失败 → 删除残留文件，显示错误信息并等待用户关闭
                    await DeleteFilePermanentlyAsync(targetFile);
                    CompleteWithFailure(progressDialog, failureMessage, ex.Message);
                    await dialogTask;
                    return false;
                }
                finally
                {
                    // 清理取消令牌资源
                    cancellationTokenSource.Dispose();
                }
            }
            catch (Exception ex)
            {
                // 外层异常（如选择器初始化失败等）→ 显示错误弹窗
                await ShowErrorDialogAsync(xamlRoot,
                    _resourceLoader.GetString("Services_FileDownload_Error"),
                    _resourceLoader.GetString("Services_FileDownload_Error_Describe") + $"\n{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 下载图片的便捷方法
        /// </summary>
        public static async Task<bool> DownloadImageAsync(string imageUrl, string suggestedFileName, XamlRoot xamlRoot)
        {
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "JPEG", new List<string> { ".jpg", ".jpeg" } },
                { "PNG", new List<string> { ".png" } },
                { "BMP", new List<string> { ".bmp" } },
                { _resourceLoader.GetString("Services_FileDownload_AllFile_Image"),
                    new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" } }
            };
            string defaultExtension = GetDefaultImageExtension(imageUrl);
            return await DownloadFileAsync(imageUrl, suggestedFileName, defaultExtension, fileTypeChoices, xamlRoot);
        }

        /// <summary>
        /// 下载视频的便捷方法
        /// </summary>
        public static async Task<bool> DownloadVideoAsync(string videoUrl, string suggestedFileName, XamlRoot xamlRoot)
        {
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "MP4", new List<string> { ".mp4" } },
                { "WebM", new List<string> { ".webm" } },
                { _resourceLoader.GetString("Services_FileDownload_AllFile_Video"),
                    new List<string> { ".mp4", ".webm", ".avi", ".mov", ".wmv", ".flv", ".mkv" } }
            };
            string defaultExtension = GetDefaultVideoExtension(videoUrl);
            return await DownloadFileAsync(videoUrl, suggestedFileName, defaultExtension, fileTypeChoices, xamlRoot);
        }

        /// <summary>
        /// 下载通用文件的便捷方法
        /// </summary>
        public static async Task<bool> DownloadGenericFileAsync(string fileUrl, string suggestedFileName, XamlRoot xamlRoot)
        {
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { _resourceLoader.GetString("Services_FileDownload_AllFile"), new List<string> { ".*" } }
            };
            string defaultExtension = Path.GetExtension(fileUrl);
            if (string.IsNullOrEmpty(defaultExtension))
                defaultExtension = ".download";
            return await DownloadFileAsync(fileUrl, suggestedFileName, defaultExtension, fileTypeChoices, xamlRoot);
        }

        // ==================== 私有辅助方法 ====================

        /// <summary>
        /// 确保默认扩展名存在于文件类型选择列表中，若不存在则添加
        /// </summary>
        private static void EnsureDefaultExtensionInChoices(ref Dictionary<string, List<string>>? fileTypeChoices, string? defaultExtension)
        {
            if (string.IsNullOrEmpty(defaultExtension) || fileTypeChoices == null)
                return;

            bool exists = fileTypeChoices.Values.Any(list => list.Contains(defaultExtension, StringComparer.OrdinalIgnoreCase));
            if (!exists)
            {
                string description = GetFileTypeDescription(defaultExtension);
                fileTypeChoices[description] = new List<string> { defaultExtension };
            }
        }

        /// <summary>
        /// 显示文件保存选择器，返回用户选择的 StorageFile
        /// </summary>
        private static async Task<StorageFile?> PickSaveFileAsync(
            string suggestedFileName,
            string? defaultExtension,
            Dictionary<string, List<string>>? fileTypeChoices)
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = suggestedFileName
            };

            if (fileTypeChoices != null)
            {
                foreach (var choice in fileTypeChoices)
                    picker.FileTypeChoices.Add(choice.Key, choice.Value);
            }
            else
            {
                picker.FileTypeChoices.Add(_resourceLoader.GetString("Services_FileDownload_AllFile"), new List<string> { ".*" });
            }

            if (!string.IsNullOrEmpty(defaultExtension))
                picker.DefaultFileExtension = defaultExtension;

            // 关联窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            return await picker.PickSaveFileAsync();
        }

        /// <summary>
        /// 创建进度对话框并返回对话框实例及取消令牌源
        /// </summary>
        private static (ContentDialog dialog, CancellationTokenSource cts) CreateProgressDialog(XamlRoot xamlRoot, string? title)
        {
            var cts = new CancellationTokenSource();

            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title ?? _resourceLoader.GetString("Services_FileDownload_Downloading"),
                Content = CreateProgressContent(),
                PrimaryButtonText = _resourceLoader.GetString("Services_FileDownload_Cancel"),
                CloseButtonText = null,
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = App.AppTheme
            };

            // 点击取消按钮 → 仅触发取消令牌，不执行其他操作
            dialog.PrimaryButtonClick += (sender, args) =>
            {
                args.Cancel = false; // 允许对话框关闭
                cts.Cancel();
            };

            return (dialog, cts);
        }

        /// <summary>
        /// 创建进度对话框的初始内容
        /// </summary>
        private static StackPanel CreateProgressContent()
        {
            return new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
                Children =
                {
                    new ProgressBar
                    {
                        IsIndeterminate = false,
                        Minimum = 0,
                        Maximum = 100,
                        Value = 0,
                        Width = 450,
                        Margin = new Thickness(0, 10, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new Grid
                    {
                        Children =
                        {
                            new TextBlock { Name = "SpeedTextBlock", HorizontalAlignment = HorizontalAlignment.Left },
                            new TextBlock { Name = "ProgressTextBlock", HorizontalAlignment = HorizontalAlignment.Right }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 核心下载逻辑：从 URL 读取流并写入文件，同时通过回调报告进度
        /// </summary>
        private static async Task DownloadStreamToFileAsync(
            string downloadUrl,
            StorageFile targetFile,
            CancellationToken cancellationToken,
            Action<long, long, long, TimeSpan?> progressCallback)
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? 0;
            long downloadedBytes = 0;
            var speedTimer = Stopwatch.StartNew();
            long lastDownloadedBytes = 0;
            long bytesPerSecond = 0;
            TimeSpan? remainingTime = null;

            await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destStream = await targetFile.OpenStreamForWriteAsync();

            byte[] buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                downloadedBytes += bytesRead;

                // 每 100ms 更新一次进度
                if (speedTimer.ElapsedMilliseconds >= 100)
                {
                    bytesPerSecond = (long)((downloadedBytes - lastDownloadedBytes) / (speedTimer.ElapsedMilliseconds / 1000.0));
                    lastDownloadedBytes = downloadedBytes;
                    speedTimer.Restart();

                    if (totalBytes > 0 && bytesPerSecond > 0)
                    {
                        long remainingBytes = totalBytes - downloadedBytes;
                        remainingTime = remainingBytes > 0
                            ? TimeSpan.FromSeconds(remainingBytes / (double)bytesPerSecond)
                            : TimeSpan.Zero;
                    }
                    else
                    {
                        remainingTime = null;
                    }

                    progressCallback(downloadedBytes, totalBytes, bytesPerSecond, remainingTime);
                }
            }

            // 最终刷新进度
            progressCallback(downloadedBytes, totalBytes, bytesPerSecond, TimeSpan.Zero);
        }

        /// <summary>
        /// 更新进度对话框上的进度条和文本
        /// </summary>
        private static void UpdateProgressUI(ContentDialog dialog, long downloaded, long total, long speed, TimeSpan? remaining)
        {
            if (dialog.Content is not StackPanel panel || panel.Children.Count < 2)
                return;

            // 进度条
            if (panel.Children[0] is ProgressBar progressBar)
            {
                if (total > 0)
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = Math.Min(100.0, downloaded * 100.0 / total);
                }
                else
                {
                    progressBar.IsIndeterminate = true;
                }
            }

            // 速度与进度文本
            if (panel.Children[1] is Grid grid && grid.Children.Count >= 2)
            {
                if (grid.Children[0] is TextBlock speedText)
                {
                    string speedStr = FormatHelper.FormatFileSize(speed) + "/s";
                    if (remaining.HasValue && remaining.Value.TotalSeconds > 0)
                    {
                        string remainingStr = FormatRemainingTime(remaining.Value);
                        speedText.Text = $"{speedStr} - " + _resourceLoader.GetString("Services_FileDownload_remainingTimeText") + remainingStr;
                    }
                    else
                    {
                        speedText.Text = speedStr;
                    }
                }

                if (grid.Children[1] is TextBlock progressText)
                {
                    string downloadedStr = FormatHelper.FormatFileSize(downloaded);
                    if (total > 0)
                    {
                        string totalStr = FormatHelper.FormatFileSize(total);
                        double percent = downloaded * 100.0 / total;
                        progressText.Text = $"{downloadedStr} / {totalStr} ({percent:F1}%)";
                    }
                    else
                    {
                        progressText.Text = downloadedStr;
                    }
                }
            }
        }

        /// <summary>
        /// 将 TimeSpan 格式化为易读的剩余时间字符串
        /// </summary>
        private static string FormatRemainingTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Hour") +
                       $"{(int)time.Minutes}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Minute");
            if (time.TotalMinutes >= 1)
                return $"{(int)time.TotalMinutes}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Minute") +
                       $"{(int)time.Seconds}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Second");
            return $"{(int)time.TotalSeconds}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Second");
        }

        /// <summary>
        /// 将对话框转换为“下载成功”状态
        /// </summary>
        private static void CompleteWithSuccess(ContentDialog dialog, string? successMessage, string filePath)
        {
            dialog.Title = _resourceLoader.GetString("Services_FileDownload_Success");
            dialog.Content = new TextBlock
            {
                Text = successMessage ?? (_resourceLoader.GetString("Services_FileDownload_FilePath") + $"\n{filePath}"),
                TextWrapping = TextWrapping.WrapWholeWords,
                MaxWidth = 450
            };
            dialog.PrimaryButtonText = _resourceLoader.GetString("Services_FileDownload_Close");
            // 移除旧的取消事件（若有），添加关闭事件
            dialog.PrimaryButtonClick -= null; // 实际应移除特定处理，但为了简单，重新赋值事件
            dialog.PrimaryButtonClick += (s, e) => dialog.Hide();
            dialog.IsPrimaryButtonEnabled = true;
        }

        /// <summary>
        /// 将对话框转换为“下载失败”状态
        /// </summary>
        private static void CompleteWithFailure(ContentDialog dialog, string? failureMessage, string errorDetail)
        {
            dialog.Title = _resourceLoader.GetString("Services_FileDownload_Failure");
            dialog.Content = new TextBlock
            {
                Text = failureMessage ?? (_resourceLoader.GetString("Services_FileDownload_Failure") + $"\n{errorDetail}"),
                TextWrapping = TextWrapping.WrapWholeWords,
                MaxWidth = 450
            };
            dialog.PrimaryButtonText = _resourceLoader.GetString("Services_FileDownload_Close");
            dialog.PrimaryButtonClick -= null;
            dialog.PrimaryButtonClick += (s, e) => dialog.Hide();
            dialog.IsPrimaryButtonEnabled = true;
        }

        /// <summary>
        /// 永久删除文件
        /// </summary>
        private static async Task DeleteFilePermanentlyAsync(StorageFile? file)
        {
            if (file == null) return;
            try
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch
            {
                // 忽略删除失败
            }
        }

        /// <summary>
        /// 显示通用错误对话框。
        /// </summary>
        private static async Task ShowErrorDialogAsync(XamlRoot xamlRoot, string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = _resourceLoader.GetString("Services_FileDownload_Close"),
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = App.AppTheme
            };
            await dialog.ShowAsync();
        }

        // 扩展名辅助方法

        private static string GetDefaultImageExtension(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return ".jpg";
            string ext = Path.GetExtension(imageUrl);
            if (!string.IsNullOrEmpty(ext))
            {
                string[] valid = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
                if (valid.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    return ext;
            }
            return ".jpg";
        }

        private static string GetDefaultVideoExtension(string videoUrl)
        {
            if (string.IsNullOrEmpty(videoUrl)) return ".mp4";
            string ext = Path.GetExtension(videoUrl);
            if (!string.IsNullOrEmpty(ext))
            {
                string[] valid = { ".mp4", ".webm", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
                if (valid.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    return ext;
            }
            return ".mp4";
        }

        private static string GetFileTypeDescription(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return _resourceLoader.GetString("Services_FileDownload_File");
            extension = extension.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "JPEG",
                ".png" => "PNG",
                ".bmp" => "BMP",
                ".gif" => "GIF",
                ".webp" => "WebP",
                ".tiff" or ".tif" => "TIFF",
                ".mp4" => "MP4",
                ".webm" => "WebM",
                ".avi" => "AVI",
                ".mov" => "MOV",
                ".wmv" => "WMV",
                ".mp3" => "MP3",
                ".wav" => "WAV",
                ".zip" => "ZIP",
                ".rar" => "RAR",
                ".7z" => "7Z",
                ".txt" => _resourceLoader.GetString("Services_FileDownload_TxtFile"),
                ".pdf" => "PDF",
                ".doc" or ".docx" => "Word",
                ".xls" or ".xlsx" => "Excel",
                _ => extension.TrimStart('.').ToUpper()
            };
        }
    }
}