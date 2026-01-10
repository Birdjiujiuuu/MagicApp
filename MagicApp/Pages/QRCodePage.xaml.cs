using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using QRCoder;
using System;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace MagicApp.Pages
{
    public sealed partial class QRCodePage : Page
    {
        public QRCodePage()
        {
            InitializeComponent();
        }

        private async void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 更新文本长度
            TextLengthTextBlock.Text = InputTextBox.Text.Length.ToString();

            // 如果文本框有内容，启用生成按钮
            GenerateButton.IsEnabled = !string.IsNullOrWhiteSpace(InputTextBox.Text);

            // 如果内容为空，清空二维码
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                QRCodeImage.Source = null;
                StatusTextBlock.Text = "等待输入...";
            }
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
                {
                    StatusTextBlock.Text = "请输入要生成二维码的内容";
                    return;
                }

                StatusTextBlock.Text = "正在生成二维码...";

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

                StatusTextBlock.Text = $"二维码生成成功 ({DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"生成失败: {ex.Message}";

                // 显示错误对话框
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "生成错误",
                    Content = $"生成二维码时出现错误：{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = Content.XamlRoot
                };

                await errorDialog.ShowAsync();
            }
        }
    }
}