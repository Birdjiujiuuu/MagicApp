using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PicturesPage : Page
    {
        public PicturesPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox.SelectedIndex = 0;
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProgressRing.IsActive = true;
            Picture.Source = null;
            Icon.Source = null;
            InfoButton.IsEnabled = false;
            DownloadButton.IsEnabled = false;

            var Web = ComboBox.SelectedItem as ComboBoxItem;
            string SourceWeb = Web?.Name?.ToString() ?? string.Empty;

            if (SourceWeb == "Bing")
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        string url = "https://cn.bing.com/HPImageArchive.aspx?n=1";
                        var response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string retString = await response.Content.ReadAsStringAsync();

                            var doc = XDocument.Parse(retString);
                            var imageElement = doc.Descendants("image").FirstOrDefault();

                            if (imageElement != null)
                            {
                                string imgurl = "http://cn.bing.com" + imageElement.Element("urlBase")?.Value + "_UHD.jpg";
                                string? StrInfoTitle = imageElement.Element("headline")?.Value;
                                string? StrInfoBody = imageElement.Element("copyright")?.Value;

                                BitmapImage Pictures = new();
                                Pictures.UriSource = new Uri(this.BaseUri, imgurl);
                                Picture.Source = Pictures;
                                BitmapImage Icons = new();
                                Icons.UriSource = new Uri(this.BaseUri, "https://cn.bing.com/favicon.ico");
                                Icon.Source = Icons;
                                InfoTitle.Text = StrInfoTitle;
                                InfoBody.Text = StrInfoBody;

                                ProgressRing.IsActive = false;
                                InfoButton.IsEnabled = true;
                                DownloadButton.IsEnabled = true;
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            else if (SourceWeb == "NASA")
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        string url = "https://api.nasa.gov/planetary/apod?api_key=ArinapLeN81diwrDMp8C3v8BYYz5eSgt9eOpXo7h";
                        var response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string retString = await response.Content.ReadAsStringAsync();

                            // 使用 JsonDocument 解析 JSON 响应
                            using JsonDocument document = JsonDocument.Parse(retString);
                            JsonElement root = document.RootElement;

                            // 安全地获取字段值
                            string? imgurl = root.TryGetProperty("hdurl", out JsonElement hdurlElement)
                                            ? hdurlElement.GetString()
                                            : string.Empty;
                            string? StrInfoTitle = root.TryGetProperty("title", out JsonElement titleElement)
                                                 ? titleElement.GetString()
                                                 : string.Empty;
                            string? StrInfoBody = root.TryGetProperty("explanation", out JsonElement explanationElement)
                                                ? explanationElement.GetString()
                                                : string.Empty;

                            BitmapImage Pictures = new();
                            Pictures.UriSource = new Uri(this.BaseUri, imgurl);
                            Picture.Source = Pictures;
                            BitmapImage Icons = new();
                            Icons.UriSource = new Uri(this.BaseUri, "https://apod.nasa.gov/favicon.ico");
                            Icon.Source = Icons;
                            InfoTitle.Text = StrInfoTitle;
                            InfoBody.Text = StrInfoBody;

                            ProgressRing.IsActive = false;
                            InfoButton.IsEnabled = true;
                            DownloadButton.IsEnabled = true;
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

        // 图片下载
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;

                try
                {
                    // 检查是否有图片可以下载
                    if (Picture.Source == null)
                    {
                        button.IsEnabled = true;
                        return;
                    }

                    // 获取当前图片的URL
                    string imageUrl = GetCurrentImageUrl();
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        button.IsEnabled = true;
                        return;
                    }

                    // 使用通用下载服务
                    string suggestedFileName = $"Wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}";

                    bool success = await FileDownloadService.DownloadImageAsync(
                        imageUrl,
                        suggestedFileName,
                        this.XamlRoot);
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }

        private string GetCurrentImageUrl()
        {
            return (Picture.Source as BitmapImage)?.UriSource?.ToString() ?? "";
        }
    }
}