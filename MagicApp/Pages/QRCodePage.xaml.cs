using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using ZXing.Windows.Compatibility;

namespace MagicApp.Pages
{
    public sealed partial class QRCodePage : Page
    {
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        // 存储上一次生成的二维码字节数组
        private byte[]? _lastQrCodeBytes;

        // 识别区：当前预览图字节 与 上一次识别结果
        private byte[]? _lastDecodedBytes;

        public QRCodePage()
        {
            InitializeComponent();

            // 默认选中生成区
            ViewSelectorBar.SelectedItem = Tab_Generate;

            UpdateActionButtonState();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateActionButtonState();

            this.Generate_InputTextBox_TextChanged(this, default!);

            // 生成区：复制二维码按钮点击事件
            Generate_CopyQRCodeButton.Action = async () =>
            {
                if (_lastQrCodeBytes is not { Length: > 0 })
                    return false;

                try
                {
                    var stream = new InMemoryRandomAccessStream();
                    await stream.WriteAsync(_lastQrCodeBytes.AsBuffer());
                    stream.Seek(0);

                    var dataPackage = new DataPackage
                    {
                        RequestedOperation = DataPackageOperation.Copy
                    };
                    // SetBitmap 会持有这个流的引用，因此不要 Dispose 它
                    dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));

                    Clipboard.SetContent(dataPackage);
                    Clipboard.Flush(); // 应用关闭后剪贴板内容仍然保留

                    return true;
                }
                catch
                {
                    return false;
                }
            };

            // 生成区：保存二维码按钮点击事件
            Generate_SaveQRCodeButton.Action = async () =>
            {
                if (_lastQrCodeBytes is not { Length: > 0 })
                    return false;

                try
                {
                    var picker = new FileSavePicker
                    {
                        SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                        SuggestedFileName = $"QRCode_{DateTime.Now:yyyyMMdd_HHmmss}"
                    };
                    picker.FileTypeChoices.Add("PNG", new List<string> { ".png" });
                    picker.DefaultFileExtension = ".png";

                    // 关联主窗口句柄
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                    var file = await picker.PickSaveFileAsync();
                    if (file == null)
                        return false; // 用户取消则播放失败动画

                    // 写入字节
                    using (var stream = await file.OpenStreamForWriteAsync())
                    {
                        stream.SetLength(0); // 覆盖已有文件
                        await stream.WriteAsync(_lastQrCodeBytes, 0, _lastQrCodeBytes.Length);
                        await stream.FlushAsync();
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            };

            // 识别区：粘贴按钮点击事件
            Decode_PasteFromClipboardButton.Action = async () =>
            {
                try
                {
                    var content = Clipboard.GetContent();

                    if (content.Contains(StandardDataFormats.Bitmap))
                    {
                        var reference = await content.GetBitmapAsync();
                        using var stream = await reference.OpenReadAsync();
                        await DecodeFromStreamAsync(stream);
                        return true;
                    }

                    if (content.Contains(StandardDataFormats.StorageItems))
                    {
                        var items = await content.GetStorageItemsAsync();
                        var file = items.OfType<StorageFile>().FirstOrDefault(f => IsImageFile(f.Name));
                        if (file != null)
                        {
                            await DecodeFromFileAsync(file);
                            return true;
                        }
                    }

                    ShowDecodeError(_resourceLoader.GetString("Pages_QRCode_Decode_Error_NoImageInPaste"));
                    return false;
                }
                catch (Exception ex)
                {
                    ShowDecodeError(_resourceLoader.GetString("Pages_QRCode_Decode_Error_PasteFailed") + ex.Message);
                    return false;
                }
            };

            // 识别区：清空按钮点击事件
            Decode_ClearButton.Action = async () =>
            {
                // 没有内容可清空返回 false
                if (_lastDecodedBytes is not { Length: > 0 })
                    return false;

                _lastDecodedBytes = null;

                Decode_PreviewImage.Source = null;
                Decode_PlaceholderText.Visibility = Visibility.Visible;

                // 重置状态为空白并禁止交互
                Decode_ResultTextBox.Text = string.Empty;
                Decode_ResultTextBox.PlaceholderText = string.Empty;
                Decode_ResultTextBox.IsHitTestVisible = false;

                return true;
            };
        }

        // ==================== 视图切换 ====================
        private void ViewSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            bool isGenerate = ReferenceEquals(sender.SelectedItem, Tab_Generate);

            GenerateView.Visibility = isGenerate ? Visibility.Visible : Visibility.Collapsed;
            DecodeView.Visibility = isGenerate ? Visibility.Collapsed : Visibility.Visible;
        }

        // ==================== 生成区 ====================

        // 更新操作按钮的状态
        private void UpdateActionButtonState()
        {
            bool hasQr = _lastQrCodeBytes is { Length: > 0 };
            Generate_CopyQRCodeButton.IsEnabled = hasQr;
            Generate_SaveQRCodeButton.IsEnabled = hasQr;
        }

        private async void Generate_InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 更新文本长度
            Generate_TextLengthData.DataValue = Generate_InputTextBox.Text.Length.ToString();

            // 如果文本框有内容，启用生成按钮
            Generate_GenerateButton.IsEnabled = !string.IsNullOrWhiteSpace(Generate_InputTextBox.Text);

            // 如果内容为空，清空二维码
            if (string.IsNullOrWhiteSpace(Generate_InputTextBox.Text))
            {
                Generate_QRCodeImage.Source = null;
                Generate_StatusTextBlock.Text = string.Empty;

                // 清空上一次生成的二维码字节数组
                _lastQrCodeBytes = null;
                UpdateActionButtonState();
            }
        }

        private void Generate_ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Generate_InputTextBox.Text = string.Empty;
        }

        private async void Generate_GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            await GenerateQRCode();
        }

        private async Task GenerateQRCode()
        {
            try
            {
                string text = Generate_InputTextBox.Text;
                if (string.IsNullOrWhiteSpace(text))
                    return;
                byte[] qrCodeBytes;

                var barcodeWriter = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Renderer = new BitmapRenderer
                    {
                        Foreground = Color.Black,
                        Background = Color.White
                    },
                    Options = new EncodingOptions
                    {
                        Width = 1500,
                        Height = 1500,
                        Margin = 2,
                    }
                };
                barcodeWriter.Options.Hints[EncodeHintType.ERROR_CORRECTION] = ErrorCorrectionLevel.Q;
                // 确保 UTF-8 编码
                barcodeWriter.Options.Hints[EncodeHintType.CHARACTER_SET] = "UTF-8";

                // 生成 System.Drawing.Bitmap 并转换为 byte[]
                using (Bitmap bitmap = barcodeWriter.Write(text))
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    qrCodeBytes = ms.ToArray(); // 得到 PNG 字节数组
                }

                // 显示预览
                using (var stream = new InMemoryRandomAccessStream())
                {
                    await stream.WriteAsync(qrCodeBytes.AsBuffer());
                    stream.Seek(0);

                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                    Generate_QRCodeImage.Source = bitmapImage;
                }

                // 保存字节，并启用操作按钮
                _lastQrCodeBytes = qrCodeBytes;
                UpdateActionButtonState();

                Generate_StatusTextBlock.Text =
                    _resourceLoader.GetString("Pages_QRCode_Generate_StatusSuccess") +
                    $" ({DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                // 生成失败则清空字节并禁用操作按钮
                _lastQrCodeBytes = null;
                UpdateActionButtonState();

                Generate_StatusTextBlock.Text =
                    _resourceLoader.GetString("Pages_QRCode_Generate_StatusFailure") +
                    $" {ex.Message}";
                Generate_QRCodeImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/None.png"));
            }
        }

        // ==================== 识别区 ====================

        // -------- 状态辅助方法（统一处理 UI 反馈） --------
        private void ShowDecodeError(string message)
        {
            Decode_ResultTextBox.Text = string.Empty; // 清空正文
            Decode_ResultTextBox.PlaceholderText = message; // 用占位符显示灰色的提示信息
            Decode_ResultTextBox.IsHitTestVisible = false; // 失败或提示时，禁止鼠标交互/框选
        }

        private void ShowDecodeSuccess(string text)
        {
            Decode_ResultTextBox.Text = text; // 显示识别结果正文
            Decode_ResultTextBox.IsHitTestVisible = true; // 成功时允许框选复制
        }

        // -------- 点击选图 --------
        private async void Decode_DropButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                    ViewMode = PickerViewMode.Thumbnail
                };
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".bmp");
                picker.FileTypeFilter.Add(".gif");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null)
                    return;

                await DecodeFromFileAsync(file);
            }
            catch (Exception ex)
            {
                ShowDecodeError(_resourceLoader.GetString("Pages_QRCode_Decode_Error_SelectImageFailed") + ex.Message);
            }
        }

        // -------- 拖拽 --------
        private void DecodePreview_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.Bitmap) ||
                e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = _resourceLoader.GetString("Pages_QRCode_Decode_Drop_Caption");
                e.DragUIOverride.IsCaptionVisible = true;
            }
        }

        private async void DecodePreview_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                if (e.DataView.Contains(StandardDataFormats.Bitmap))
                {
                    var reference = await e.DataView.GetBitmapAsync();
                    using var stream = await reference.OpenReadAsync();
                    await DecodeFromStreamAsync(stream);
                    return;
                }

                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    var file = items.OfType<StorageFile>().FirstOrDefault(f => IsImageFile(f.Name));
                    if (file != null)
                    {
                        await DecodeFromFileAsync(file);
                        return;
                    }
                }

                ShowDecodeError(_resourceLoader.GetString("Pages_QRCode_Decode_Error_NoImageInDrop"));
            }
            catch (Exception ex)
            {
                ShowDecodeError(_resourceLoader.GetString("Pages_QRCode_Decode_Error_DropFailed") + ex.Message);
            }
            finally
            {
                deferral.Complete();
            }
        }

        // -------- 解码流程 --------
        private async Task DecodeFromFileAsync(StorageFile file)
        {
            using var stream = await file.OpenReadAsync();
            await DecodeFromStreamAsync(stream);
        }

        private async Task DecodeFromStreamAsync(IRandomAccessStream stream)
        {
            try
            {
                // 先把流完整读入内存
                byte[] bytes;
                using (var memory = new MemoryStream())
                {
                    using (var source = stream.AsStreamForRead())
                    {
                        await source.CopyToAsync(memory);
                    }
                    bytes = memory.ToArray();
                }

                if (bytes.Length == 0)
                {
                    ShowDecodeError(_resourceLoader.GetString("Pages_QRCode_Decode_Error_ImageContentEmpty"));
                    return;
                }

                await DecodeFromBytesAsync(bytes);
            }
            catch (Exception ex)
            {
                ShowDecodeError(_resourceLoader.GetString("Pages_QRCode_Decode_Error_ReadImageFailed") + ex.Message);
            }
        }

        private async Task DecodeFromBytesAsync(byte[] bytes)
        {
            // 开始识别并禁用框选
            Decode_ResultTextBox.IsHitTestVisible = false;

            // 预览
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(bytes.AsBuffer());
                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                Decode_PreviewImage.Source = bitmapImage;
                Decode_PlaceholderText.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // 预览失败不阻止继续尝试解码
                Decode_PreviewImage.Source = null;
                Decode_PlaceholderText.Visibility = Visibility.Visible;
            }

            _lastDecodedBytes = bytes;

            // 解码放到后台线程
            string? text = null;
            string? error = null;
            try
            {
                text = await Task.Run(() => DecodeQrCodeFromBytes(bytes));
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            // 根据结果更新 UI
            if (!string.IsNullOrEmpty(text))
            {
                ShowDecodeSuccess(text);
            }
            else
            {
                ShowDecodeError(error == null ? _resourceLoader.GetString("Pages_QRCode_Decode_Error_NoQrCodeFound") : _resourceLoader.GetString("Pages_QRCode_Decode_Error_DecodeFailed") + error);
            }
        }

        /// <summary>
        /// 纯解码逻辑（在后台线程执行）。
        /// </summary>
        private static string? DecodeQrCodeFromBytes(byte[] imageBytes)
        {
            using var inputStream = new MemoryStream(imageBytes);
            using var source = new Bitmap(inputStream);

            // 把透明区域铺成白底，避免透明 PNG 被当成黑块导致识别失败
            using var flattened = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(flattened))
            {
                graphics.Clear(Color.White);
                graphics.DrawImage(source, 0, 0, source.Width, source.Height);
            }

            var reader = new BarcodeReader
            {
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                },
                AutoRotate = true
            };

            var result = reader.Decode(flattened);
            return result?.Text;
        }

        private static bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif";
        }
    }
}