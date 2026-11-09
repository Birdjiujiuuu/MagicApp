using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace MagicApp.Pages
{
    public sealed partial class QRCodePage : Page
    {
        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        // 存储上一次生成的二维码字节数组
        private byte[]? _lastQrCodeBytes;

        public QRCodePage()
        {
            InitializeComponent();

            UpdateActionButtonState();
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
                byte[] qrCodeBytes;

                // 生成 PNG 字节
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q))
                {
                    var qrCode = new PngByteQRCode(qrCodeData);
                    qrCodeBytes = qrCode.GetGraphic(20);
                }

                // 显示到 Image
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
    }
}