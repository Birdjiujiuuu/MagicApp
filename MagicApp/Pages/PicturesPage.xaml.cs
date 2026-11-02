using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MagicApp.Pages
{
    public sealed partial class PicturesPage : Page
    {
        private bool _isLoaded;

        public PicturesPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                return;
            }
            _isLoaded = true;

            BitmapImage BingIcons = new();
            BingIcons.UriSource = new Uri(this.BaseUri, "https://www.bing.com/favicon.ico");
            BingIcon.Source = BingIcons;

            await LoadBingImageAsync();
        }

        private async Task LoadBingImageAsync()
        {
            ProgressRing.IsActive = true;
            Picture.Source = null;
            InfoButton.IsEnabled = false;
            DownloadButton.IsEnabled = false;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string url = "https://www.bing.com/HPImageArchive.aspx?n=1";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string retString = await response.Content.ReadAsStringAsync();

                        var doc = XDocument.Parse(retString);
                        var imageElement = doc.Descendants("image").FirstOrDefault();

                        if (imageElement != null)
                        {
                            string imgurl = "http://www.bing.com" + imageElement.Element("urlBase")?.Value + "_UHD.jpg";
                            string? StrInfoTitle = imageElement.Element("headline")?.Value;
                            string? StrInfoBody = imageElement.Element("copyright")?.Value;

                            BitmapImage Pictures = new();
                            Pictures.UriSource = new Uri(this.BaseUri, imgurl);
                            Picture.Source = Pictures;
                            InfoTitle.Text = StrInfoTitle;
                            InfoBody.Text = StrInfoBody;

                            InfoButton.IsEnabled = true;
                            DownloadButton.IsEnabled = true;
                        }
                    }
                }
                catch
                {

                }
                finally
                {
                    ProgressRing.IsActive = false;
                }
            }
        }

        // 图片下载
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadButton.IsEnabled = false;

            try
            {
                // 检查是否有图片可以下载
                if (Picture.Source == null)
                {
                    DownloadButton.IsEnabled = true;
                    return;
                }

                // 获取当前图片的URL
                string imageUrl = GetCurrentImageUrl();
                if (string.IsNullOrEmpty(imageUrl))
                {
                    DownloadButton.IsEnabled = true;
                    return;
                }

                // 调用FileDownloadService
                string suggestedFileName = $"Bing_Wallpaper_{DateTime.Now:yyyyMMdd}";

                await FileDownloadService.DownloadImageAsync(
                    imageUrl,
                    suggestedFileName,
                    this.XamlRoot);
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }
        }

        private string GetCurrentImageUrl()
        {
            return (Picture.Source as BitmapImage)?.UriSource?.ToString() ?? "";
        }
    }
}