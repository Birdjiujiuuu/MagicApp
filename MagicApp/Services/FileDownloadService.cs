using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MagicApp.Services
{
    public class FileDownloadService
    {

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
            var loader = ResourceLoader.GetForViewIndependentUse();

            try
            {
                // 验证参数
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    throw new ArgumentException(loader.GetString("FileDownloadService_UrlNoNull"));
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
                    picker.FileTypeChoices.Add(loader.GetString("FileDownloadService_AllFile"), new List<string> { ".*" });
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
                        Title = dialogTitle ?? loader.GetString("FileDownloadService_Downloading"),
                        Content = CreateProgressContent(),
                        PrimaryButtonText = loader.GetString("FileDownloadService_Close"),
                        IsPrimaryButtonEnabled = false,
                        CloseButtonText = null,
                        DefaultButton = ContentDialogButton.Primary
                    };

                    var showTask = progressDialog.ShowAsync();

                    try
                    {
                        // 下载文件
                        using (var downloadClient = new HttpClient())
                        {
                            var fileData = await downloadClient.GetByteArrayAsync(downloadUrl);
                            await FileIO.WriteBytesAsync(file, fileData);

                            // 下载成功，更新对话框
                            UpdateDialogForCompletion(
                                progressDialog,
                                loader.GetString("FileDownloadService_Success"),
                                successMessage ?? loader.GetString("FileDownloadService_FilePath") + $"\n{file.Path}");
                            progressDialog.IsPrimaryButtonEnabled = true;
                            await showTask;
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // 如果下载失败，尝试删除已创建的文件
                            await file.DeleteAsync();
                        }
                        catch
                        {
                            // 忽略删除错误
                        }

                        // 下载失败，更新对话框
                        UpdateDialogForCompletion(
                            progressDialog,
                            loader.GetString("FileDownloadService_Failure"),
                            failureMessage ?? loader.GetString("FileDownloadService_Failure") + $"\n{ex.Message}");
                        progressDialog.IsPrimaryButtonEnabled = true;
                        await showTask;

                        return false;
                    }
                }

                return false; // 用户取消了选择
            }
            catch (Exception ex)
            {
                // 显示错误对话框
                await ShowErrorDialogAsync(xamlRoot, loader.GetString("FileDownloadService_Error"), loader.GetString("FileDownloadService_Error_Describe") + $"\n{ex.Message}");
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
            var loader = ResourceLoader.GetForViewIndependentUse();

            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "JPEG", new List<string> { ".jpg", ".jpeg" } },
                { "PNG", new List<string> { ".png" } },
                { "BMP", new List<string> { ".bmp" } },
                { loader.GetString("FileDownloadService_AllFile_Image"), new List<string> { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tiff" } }
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
            var loader = ResourceLoader.GetForViewIndependentUse();

            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "MP4", new List<string> { ".mp4" } },
                { "WebM", new List<string> { ".webm" } },
                { loader.GetString("FileDownloadService_AllFile_Video"), new List<string> { ".mp4", ".webm", ".avi", ".mov", ".wmv", ".flv", ".mkv" } }
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
            var loader = ResourceLoader.GetForViewIndependentUse();

            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { loader.GetString("FileDownloadService_AllFile"), new List<string> { ".*" } }
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
            var loader = ResourceLoader.GetForViewIndependentUse();

            if (string.IsNullOrEmpty(extension))
                return loader.GetString("FileDownloadService_File");

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
                ".txt" => loader.GetString("FileDownloadService_TxtFile"),
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
            var loader = ResourceLoader.GetForViewIndependentUse();

            return new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 10,
                Children =
                {
                    new ProgressBar
                    {
                        IsIndeterminate = true,
                        Width = 250,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = loader.GetString("FileDownloadService_Downloading_Describe"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 0)
                    }
                }
            };
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
                MaxWidth = 400
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
                CloseButtonText = loader.GetString("FileDownloadService_Close"),
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }
    }
}