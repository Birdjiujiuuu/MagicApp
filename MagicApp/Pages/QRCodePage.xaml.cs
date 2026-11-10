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
        private string? _lastDecodedText;

        public QRCodePage()
        {
            InitializeComponent();

            // 默认选中生成区
            ViewSelectorBar.SelectedItem = GenerateTab;

            UpdateActionButtonState();
            UpdateDecodeButtonState();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateActionButtonState();

            this.InputTextBox_TextChanged(this, default!);

            // 复制二维码按钮点击事件
            CopyQRCodeButton.Action = async () =>
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

            // 保存二维码按钮点击事件
            SaveQRCodeButton.Action = async () =>
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
        }

        // ==================== 生成区 ====================

        // 更新操作按钮的状态
        private void UpdateActionButtonState()
        {
            bool hasQr = _lastQrCodeBytes is { Length: > 0 };
            CopyQRCodeButton.IsEnabled = hasQr;
            SaveQRCodeButton.IsEnabled = hasQr;
        }

        private async void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 更新文本长度
            TextLengthData.DataValue = InputTextBox.Text.Length.ToString();

            // 如果文本框有内容，启用生成按钮
            GenerateButton.IsEnabled = !string.IsNullOrWhiteSpace(InputTextBox.Text);

            // 如果内容为空，清空二维码
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                QRCodeImage.Source = null;
                StatusTextBlock.Text = string.Empty;

                // 清空上一次生成的二维码字节数组
                _lastQrCodeBytes = null;
                UpdateActionButtonState();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = string.Empty;
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            await GenerateQRCode();
        }

        private async Task GenerateQRCode()
        {
            try
            {
                string text = InputTextBox.Text;
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
                    QRCodeImage.Source = bitmapImage;
                }

                // 保存字节，并启用操作按钮
                _lastQrCodeBytes = qrCodeBytes;
                UpdateActionButtonState();

                StatusTextBlock.Text =
                    _resourceLoader.GetString("Pages_QRCode_Status_Success") +
                    $" ({DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                // 生成失败则清空字节并禁用操作按钮
                _lastQrCodeBytes = null;
                UpdateActionButtonState();

                StatusTextBlock.Text =
                    _resourceLoader.GetString("Pages_QRCode_Status_Failure") +
                    $" {ex.Message}";
                QRCodeImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/None.png"));
            }
        }

        // ==================== 识别区 ====================

        private void ViewSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            bool isGenerate = ReferenceEquals(sender.SelectedItem, GenerateTab);

            GenerateView.Visibility = isGenerate ? Visibility.Visible : Visibility.Collapsed;
            DecodeView.Visibility = isGenerate ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateDecodeButtonState()
        {
            ClearDecodeButton.IsEnabled = _lastDecodedBytes is { Length: > 0 };
        }

        // -------- 点击选图 --------
        private async void DecodeDropButton_Click(object sender, RoutedEventArgs e)
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
                DecodeStatusTextBlock.Text = "选择图片失败：" + ex.Message;
            }
        }

        // -------- 从剪贴板粘贴 --------

        private async void PasteFromClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var content = Clipboard.GetContent();

                if (content.Contains(StandardDataFormats.Bitmap))
                {
                    var reference = await content.GetBitmapAsync();
                    using var stream = await reference.OpenReadAsync();
                    await DecodeFromStreamAsync(stream);
                    return;
                }

                if (content.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await content.GetStorageItemsAsync();
                    var file = items.OfType<StorageFile>().FirstOrDefault(f => IsImageFile(f.Name));
                    if (file != null)
                    {
                        await DecodeFromFileAsync(file);
                        return;
                    }
                }

                DecodeStatusTextBlock.Text = "剪贴板中没有可识别的图片";
            }
            catch (Exception ex)
            {
                DecodeStatusTextBlock.Text = "粘贴失败：" + ex.Message;
            }
        }

        // -------- 拖拽 --------

        private void DecodePreview_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.Bitmap) ||
                e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "松开以识别二维码";
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

                DecodeStatusTextBlock.Text = "拖入的内容中没有可识别的图片";
            }
            catch (Exception ex)
            {
                DecodeStatusTextBlock.Text = "拖拽识别失败：" + ex.Message;
            }
            finally
            {
                deferral.Complete();
            }
        }

        // -------- 清空 --------
        private void ClearDecodeButton_Click(object sender, RoutedEventArgs e)
        {
            _lastDecodedBytes = null;
            _lastDecodedText = null; // 若已删除此字段可忽略

            DecodePreviewImage.Source = null;
            DecodePlaceholderText.Visibility = Visibility.Visible;
            DecodeResultTextBox.Text = string.Empty;
            DecodeStatusTextBlock.Text = string.Empty;

            UpdateDecodeButtonState();
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
                    DecodeStatusTextBlock.Text = "图片内容为空";
                    return;
                }

                await DecodeFromBytesAsync(bytes);
            }
            catch (Exception ex)
            {
                DecodeStatusTextBlock.Text = "读取图片失败：" + ex.Message;
            }
        }

        private async Task DecodeFromBytesAsync(byte[] bytes)
        {
            DecodeStatusTextBlock.Text = "正在识别…";

            // 预览
            try
            {
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(bytes.AsBuffer());
                stream.Seek(0);

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                DecodePreviewImage.Source = bitmapImage;
                DecodePlaceholderText.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // 预览失败不阻止继续尝试解码
                DecodePreviewImage.Source = null;
                DecodePlaceholderText.Visibility = Visibility.Visible;
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

            if (!string.IsNullOrEmpty(text))
            {
                DecodeResultTextBox.Text = text;
                DecodeStatusTextBlock.Text = $"识别成功 ({DateTime.Now:HH:mm:ss})";
            }
            else
            {
                DecodeResultTextBox.Text = string.Empty;
                DecodeStatusTextBlock.Text = error == null ? "未识别到二维码" : "识别失败：" + error;
            }
            UpdateDecodeButtonState();
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