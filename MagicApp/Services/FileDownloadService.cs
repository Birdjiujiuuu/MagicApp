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
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MagicApp.Services
{
    public class FileDownloadService
    {
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// 通用的文件下载方法
        /// </summary>
        /// <param name="downloadUrl">要下载的文件URL</param>
        /// <param name="suggestedFileName">建议的文件名（不含扩展名）</param>
        /// <param name="defaultExtension">默认扩展名（如：.jpg, .png, .mp4, .txt）</param>
        /// <param name="fileTypeChoices">文件类型选择列表</param>
        /// <param name="xamlRoot">用于显示对话框的XamlRoot</param>
        /// <param name="dialogTitle">对话框标题</param>
        /// <param name="successMessage">下载成功消息（可选）</param>
        /// <param name="failureMessage">下载失败消息（可选）</param>
        /// <returns>成功返回true，失败返回false</returns>
        public static async Task<bool> DownloadFileAsync(
            string downloadUrl,
            string suggestedFileName,
            string defaultExtension,
            Dictionary<string, List<string>> fileTypeChoices,
            XamlRoot xamlRoot,
            string? dialogTitle = null,
            string? successMessage = null,
            string? failureMessage = null)
        {
            try
            {
                // 验证参数
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    throw new ArgumentException(_resourceLoader.GetString("Services_FileDownload_UrlNoNull"));
                }

                if (string.IsNullOrEmpty(suggestedFileName))
                {
                    suggestedFileName = $"Download_{DateTime.Now:yyyyMMdd_HHmmss}";
                }

                // 确保默认扩展名以点开头
                if (!string.IsNullOrEmpty(defaultExtension) && !defaultExtension.StartsWith("."))
                {
                    defaultExtension = "." + defaultExtension;
                }

                // 如果提供了默认扩展名，确保它在文件类型选择中
                if (!string.IsNullOrEmpty(defaultExtension) && fileTypeChoices != null)
                {
                    bool extensionExists = false;
                    foreach (var choice in fileTypeChoices)
                    {
                        if (choice.Value.Contains(defaultExtension, StringComparer.OrdinalIgnoreCase))
                        {
                            extensionExists = true;
                            break;
                        }
                    }

                    if (!extensionExists && defaultExtension != null)
                    {
                        // 如果默认扩展名不在选择列表中，添加它
                        string description = GetFileTypeDescription(defaultExtension);
                        if (!fileTypeChoices.ContainsKey(description))
                        {
                            fileTypeChoices[description] = new List<string> { defaultExtension };
                        }
                    }
                }

                // 创建文件选择器
                var picker = new FileSavePicker();

                // 配置 FileSavePicker 属性
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.SuggestedFileName = suggestedFileName;

                // 添加文件类型选择
                if (fileTypeChoices != null)
                {
                    foreach (var choice in fileTypeChoices)
                    {
                        picker.FileTypeChoices.Add(choice.Key, choice.Value);
                    }
                }
                else
                {
                    // 如果没有提供文件类型选择，使用通用类型
                    picker.FileTypeChoices.Add(_resourceLoader.GetString("Services_FileDownload_AllFile"), new List<string> { ".*" });
                }

                // 设置默认扩展名
                if (!string.IsNullOrEmpty(defaultExtension))
                {
                    picker.DefaultFileExtension = defaultExtension;
                }

                // 关联窗口句柄
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // 显示选择器对话框
                var file = await picker.PickSaveFileAsync();

                if (file != null)
                {
                    // 创建并显示进度对话框
                    ContentDialog progressDialog = new()
                    {
                        XamlRoot = xamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = dialogTitle ?? _resourceLoader.GetString("Services_FileDownload_Downloading"),
                        Content = CreateProgressContent(),
                        PrimaryButtonText = _resourceLoader.GetString("Services_FileDownload_Cancel"),
                        CloseButtonText = null,
                        DefaultButton = ContentDialogButton.Primary
                    };

                    // 添加取消标记
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    StorageFile targetFile = file; // 保存文件引用用于取消时删除
                    bool isDownloadCompleted = false; // 添加下载完成标志

                    // 定义取消按钮的事件处理程序
                    TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> cancelHandler = async (sender, args) =>
                    {
                        // 如果下载已完成，则不执行取消逻辑
                        if (isDownloadCompleted)
                        {
                            progressDialog.Hide();
                            return;
                        }

                        args.Cancel = true; // 防止对话框立即关闭
                        cancellationTokenSource.Cancel();

                        // 延迟关闭对话框，确保删除操作完成
                        await Task.Delay(100);
                        progressDialog.Hide();

                        try
                        {
                            // 删除已下载的部分文件
                            if (targetFile != null)
                            {
                                await targetFile.DeleteAsync(StorageDeleteOption.Default);
                            }
                        }
                        catch
                        {
                            // 忽略删除错误
                        }
                    };

                    // 为取消按钮添加点击事件
                    progressDialog.PrimaryButtonClick += cancelHandler;

                    var showTask = progressDialog.ShowAsync();

                    try
                    {
                        // 使用支持取消的下载方式
                        using (var downloadClient = new HttpClient())
                        using (var response = await downloadClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token))
                        {
                            // 获取文件总大小
                            long? contentLength = response.Content.Headers.ContentLength;
                            long totalBytes = contentLength ?? 0;
                            long downloadedBytes = 0;

                            // 速度计算变量
                            Stopwatch speedTimer = Stopwatch.StartNew();
                            long lastDownloadedBytes = 0;
                            long bytesPerSecond = 0;
                            TimeSpan? remainingTime = null;

                            using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                            using (var streamToWriteTo = await targetFile.OpenStreamForWriteAsync())
                            {
                                var buffer = new byte[81920];
                                int bytesRead;

                                // 使用CancellationToken监听取消请求
                                while ((bytesRead = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token)) > 0)
                                {
                                    // 如果取消标记被请求，抛出异常
                                    cancellationTokenSource.Token.ThrowIfCancellationRequested();

                                    await streamToWriteTo.WriteAsync(buffer, 0, bytesRead, cancellationTokenSource.Token);

                                    // 更新已下载字节数
                                    downloadedBytes += bytesRead;

                                    // 计算下载速度（每0.5秒更新一次）
                                    if (speedTimer.ElapsedMilliseconds >= 500)
                                    {
                                        bytesPerSecond = (long)((downloadedBytes - lastDownloadedBytes) / (speedTimer.ElapsedMilliseconds / 1000.0));
                                        lastDownloadedBytes = downloadedBytes;
                                        speedTimer.Restart();

                                        // 计算预计剩余时间（如果总大小已知且速度>0）
                                        if (totalBytes > 0 && bytesPerSecond > 0)
                                        {
                                            long remainingBytes = totalBytes - downloadedBytes;
                                            if (remainingBytes > 0)
                                            {
                                                remainingTime = TimeSpan.FromSeconds(remainingBytes / (double)bytesPerSecond);
                                            }
                                            else
                                            {
                                                remainingTime = TimeSpan.Zero;
                                            }
                                        }
                                        else
                                        {
                                            remainingTime = null;
                                        }

                                        // 更新进度显示
                                        UpdateDownloadProgress(progressDialog, downloadedBytes, totalBytes, bytesPerSecond, remainingTime);
                                    }
                                }
                            }
                        }

                        // 下载成功，设置完成标志
                        isDownloadCompleted = true;

                        // 更新对话框
                        UpdateDialogForCompletion(
                            progressDialog,
                            _resourceLoader.GetString("Services_FileDownload_Success"),
                            successMessage ?? _resourceLoader.GetString("Services_FileDownload_FilePath") + $"\n{targetFile.Path}");

                        // 移除取消事件处理程序
                        progressDialog.PrimaryButtonClick -= cancelHandler;

                        // 添加新的关闭按钮事件处理程序
                        progressDialog.PrimaryButtonClick += (sender, args) =>
                        {
                            progressDialog.Hide();
                        };

                        // 修改按钮文本
                        progressDialog.PrimaryButtonText = _resourceLoader.GetString("Services_FileDownload_Close");

                        // 等待用户点击关闭按钮
                        await showTask;
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                        // 用户取消了下载，已处理，直接返回false
                        return false;
                    }
                    catch (Exception ex)
                    {
                        // 设置完成标志（虽然是失败，但已经完成）
                        isDownloadCompleted = true;

                        // 取消标记可能已被处理，避免重复操作
                        if (!cancellationTokenSource.IsCancellationRequested)
                        {
                            try
                            {
                                // 如果下载失败，尝试删除已创建的文件
                                await targetFile.DeleteAsync();
                            }
                            catch
                            {
                                // 忽略删除错误
                            }

                            // 移除取消事件处理程序
                            progressDialog.PrimaryButtonClick -= cancelHandler;

                            // 添加新的关闭按钮事件处理程序
                            progressDialog.PrimaryButtonClick += (sender, args) =>
                            {
                                progressDialog.Hide();
                            };

                            // 修改按钮文本
                            progressDialog.PrimaryButtonText = _resourceLoader.GetString("Services_FileDownload_Close");

                            // 下载失败，更新对话框
                            UpdateDialogForCompletion(
                                progressDialog,
                                _resourceLoader.GetString("Services_FileDownload_Failure"),
                                failureMessage ?? _resourceLoader.GetString("Services_FileDownload_Failure") + $"\n{ex.Message}");

                            progressDialog.IsPrimaryButtonEnabled = true;
                            await showTask;
                        }

                        return false;
                    }
                }

                return false; // 用户取消了选择
            }
            catch (Exception ex)
            {
                // 显示错误对话框
                await ShowErrorDialogAsync(xamlRoot, _resourceLoader.GetString("Services_FileDownload_Error"), _resourceLoader.GetString("Services_FileDownload_Error_Describe") + $"\n{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 下载图片文件的便捷方法
        /// </summary>
        public static async Task<bool> DownloadImageAsync(
            string imageUrl,
            string suggestedFileName,
            XamlRoot xamlRoot)
        {
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "JPEG", new List<string> { ".jpg", ".jpeg" } },
                { "PNG", new List<string> { ".png" } },
                { "BMP", new List<string> { ".bmp" } },
                { _resourceLoader.GetString("Services_FileDownload_AllFile_Image"), new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" } }
            };

            string defaultExtension = GetDefaultImageExtension(imageUrl);

            return await DownloadFileAsync(
                imageUrl,
                suggestedFileName,
                defaultExtension,
                fileTypeChoices,
                xamlRoot);
        }

        /// <summary>
        /// 下载视频文件的便捷方法
        /// </summary>
        public static async Task<bool> DownloadVideoAsync(
            string videoUrl,
            string suggestedFileName,
            XamlRoot xamlRoot)
        {
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "MP4", new List<string> { ".mp4" } },
                { "WebM", new List<string> { ".webm" } },
                { _resourceLoader.GetString("Services_FileDownload_AllFile_Video"), new List<string> { ".mp4", ".webm", ".avi", ".mov", ".wmv", ".flv", ".mkv" } }
            };

            string defaultExtension = GetDefaultVideoExtension(videoUrl);

            return await DownloadFileAsync(
                videoUrl,
                suggestedFileName,
                defaultExtension,
                fileTypeChoices,
                xamlRoot);
        }

        /// <summary>
        /// 下载通用文件的便捷方法
        /// </summary>
        public static async Task<bool> DownloadGenericFileAsync(
            string fileUrl,
            string suggestedFileName,
            XamlRoot xamlRoot)
        {
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { _resourceLoader.GetString("Services_FileDownload_AllFile"), new List<string> { ".*" } }
            };

            string defaultExtension = Path.GetExtension(fileUrl);
            if (string.IsNullOrEmpty(defaultExtension))
            {
                defaultExtension = ".download";
            }

            return await DownloadFileAsync(
                fileUrl,
                suggestedFileName,
                defaultExtension,
                fileTypeChoices,
                xamlRoot);
        }

        /// <summary>
        /// 根据URL获取默认的图片扩展名
        /// </summary>
        private static string GetDefaultImageExtension(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return ".jpg";

            string url = imageUrl.ToLowerInvariant();
            string extension = Path.GetExtension(url);

            if (!string.IsNullOrEmpty(extension))
            {
                // 检查是否是有效的图片扩展名
                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" };
                foreach (var ext in imageExtensions)
                {
                    if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                        return ext;
                }
            }

            // 默认返回.jpg
            return ".jpg";
        }

        /// <summary>
        /// 根据URL获取默认的视频扩展名
        /// </summary>
        private static string GetDefaultVideoExtension(string videoUrl)
        {
            if (string.IsNullOrEmpty(videoUrl))
                return ".mp4";

            string url = videoUrl.ToLowerInvariant();
            string extension = Path.GetExtension(url);

            if (!string.IsNullOrEmpty(extension))
            {
                // 检查是否是有效的视频扩展名
                string[] videoExtensions = { ".mp4", ".webm", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
                foreach (var ext in videoExtensions)
                {
                    if (extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                        return ext;
                }
            }

            // 默认返回.mp4
            return ".mp4";
        }

        /// <summary>
        /// 根据扩展名获取文件类型描述
        /// </summary>
        private static string GetFileTypeDescription(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return _resourceLoader.GetString("Services_FileDownload_File");

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
                _ => $"{extension.ToUpper().TrimStart('.')}"
            };
        }

        /// <summary>
        /// 创建进度对话框内容
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
                            new TextBlock
                            {
                                Name = "SpeedTextBlock",
                                HorizontalAlignment = HorizontalAlignment.Left,
                            },
                            new TextBlock
                            {
                                Name = "ProgressTextBlock",
                                HorizontalAlignment = HorizontalAlignment.Right,
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 更新下载进度
        /// </summary>
        private static void UpdateDownloadProgress(ContentDialog dialog, long downloadedBytes, long totalBytes, long bytesPerSecond, TimeSpan? remainingTime = null)
        {
            if (dialog.Content is StackPanel stackPanel && stackPanel.Children.Count > 1)
            {
                // 更新进度条
                if (stackPanel.Children[0] is ProgressBar progressBar)
                {
                    if (totalBytes > 0)
                    {
                        // 已知总大小，显示确定进度
                        progressBar.IsIndeterminate = false;
                        double progress = (double)downloadedBytes / totalBytes * 100;
                        progressBar.Value = Math.Min(progress, 100);
                    }
                    else
                    {
                        // 未知总大小，显示不确定进度
                        progressBar.IsIndeterminate = true;
                    }
                }

                // 更新进度文本和速度文本
                if (stackPanel.Children[1] is Grid grid && grid.Children.Count >= 2)
                {
                    // 获取速度文本块
                    if (grid.Children[0] is TextBlock speedTextBlock)
                    {
                        string speedText = FormatHelper.FormatFileSize(bytesPerSecond) + "/s";

                        if (remainingTime.HasValue && remainingTime.Value.TotalSeconds > 0)
                        {
                            // 显示速度和预计剩余时间
                            string remainingTimeText;
                            if (remainingTime.Value.TotalHours >= 1)
                            {
                                remainingTimeText = $"{(int)remainingTime.Value.TotalHours}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Hour") + $"{ (int)remainingTime.Value.Minutes}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Minute");
                            }
                            else if (remainingTime.Value.TotalMinutes >= 1)
                            {
                                remainingTimeText = $"{(int)remainingTime.Value.TotalMinutes}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Minute") + $"{ (int)remainingTime.Value.Seconds}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Second");
                            }
                            else
                            {
                                remainingTimeText = $"{(int)remainingTime.Value.TotalSeconds}" + _resourceLoader.GetString("Services_FileDownload_remainingTime_Second");
                            }

                            speedTextBlock.Text = $"{speedText} - " + _resourceLoader.GetString("Services_FileDownload_remainingTimeText") + $"{remainingTimeText}";
                        }
                        else
                        {
                            // 只显示速度
                            speedTextBlock.Text = $"{speedText}";
                        }
                    }

                    // 获取进度文本块
                    if (grid.Children[1] is TextBlock progressTextBlock)
                    {
                        string downloadedSize = FormatHelper.FormatFileSize(downloadedBytes);

                        if (totalBytes > 0)
                        {
                            // 已知总大小
                            string totalSize = FormatHelper.FormatFileSize(totalBytes);
                            double progress = (double)downloadedBytes / totalBytes * 100;
                            progressTextBlock.Text = $"{downloadedSize} / {totalSize} ({progress:F1}%)";
                        }
                        else
                        {
                            // 未知总大小
                            progressTextBlock.Text = $"{downloadedSize}";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新对话框为完成状态
        /// </summary>
        private static void UpdateDialogForCompletion(ContentDialog dialog, string title, string message)
        {
            dialog.Title = title;
            dialog.Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWholeWords,
                MaxWidth = 450
            };
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        private static async Task ShowErrorDialogAsync(XamlRoot xamlRoot, string title, string message)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            var dialog = new ContentDialog
            {
                XamlRoot = xamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = loader.GetString("Services_FileDownload_Close"),
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }
    }
}