using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;

namespace MagicApp.Pages
{
    public sealed partial class QRCodePage : Page
    {
        public QRCodePage()
        {
            InitializeComponent();
        }

        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.InputTextBox_TextChanged(this, default!);
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

                // 使用QRCoder生成二维码
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);

                    // 使用PngByteQRCode来生成PNG格式的二维码
                    PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);

                    // 获取二维码的字节数组
                    byte[] qrCodeImage = qrCode.GetGraphic(20);

                    // 将字节数组转换为BitmapImage
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        await stream.WriteAsync(qrCodeImage.AsBuffer());
                        stream.Seek(0);

                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);

                        QRCodeImage.Source = bitmapImage;
                    }
                }

                StatusTextBlock.Text = _resourceLoader.GetString("QRCode_Status_Success") + $" ({DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                // 处理生成二维码时的异常
                StatusTextBlock.Text = _resourceLoader.GetString("QRCode_Status_Failure") + $" {ex.Message}";
                QRCodeImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/None.png"));
            }
        }
    }
}