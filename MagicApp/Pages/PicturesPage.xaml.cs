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

                                BitmapImage Pictures = new BitmapImage();
                                Pictures.UriSource = new Uri(this.BaseUri, imgurl);
                                Picture.Source = Pictures;
                                BitmapImage Icons = new BitmapImage();
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

                            // ʹ�� JsonDocument ���� JSON ��Ӧ
                            using JsonDocument document = JsonDocument.Parse(retString);
                            JsonElement root = document.RootElement;

                            // ��ȫ�ػ�ȡ�ֶ�ֵ
                            string? imgurl = root.TryGetProperty("hdurl", out JsonElement hdurlElement)
                                            ? hdurlElement.GetString()
                                            : string.Empty;
                            string? StrInfoTitle = root.TryGetProperty("title", out JsonElement titleElement)
                                                 ? titleElement.GetString()
                                                 : string.Empty;
                            string? StrInfoBody = root.TryGetProperty("explanation", out JsonElement explanationElement)
                                                ? explanationElement.GetString()
                                                : string.Empty;

                            BitmapImage Pictures = new BitmapImage();
                            Pictures.UriSource = new Uri(this.BaseUri, imgurl);
                            Picture.Source = Pictures;
                            BitmapImage Icons = new BitmapImage();
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

        // ͼƬ����
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            if (sender is Button button)
            {
                button.IsEnabled = false;

                // ����Ƿ���ͼƬ��������
                if (Picture.Source == null)
                {
                    button.IsEnabled = true;
                    return;
                }

                // ��ȡ��ǰͼƬ��URL
                string imageUrl = GetCurrentImageUrl();
                if (string.IsNullOrEmpty(imageUrl))
                {
                    button.IsEnabled = true;
                    return;
                }

                var picker = new FileSavePicker();

                // ���� FileSavePicker ����
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.SuggestedFileName = $"Wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}";
                picker.FileTypeChoices.Add("JPEG", new List<string>() { ".jpg" });
                picker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });

                // �������ھ��
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                // ��ʾѡ�����Ի���
                var file = await picker.PickSaveFileAsync();

                if (file != null)
                {
                    // ��������ʾ���ȶԻ���
                    ContentDialog progressDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = loader.GetString("Pictures_Download_Dialog_Downloading"),
                        Content = CreateProgressContent(),
                        PrimaryButtonText = loader.GetString("Pictures_Download_Dialog_Close"),
                        IsPrimaryButtonEnabled = false,
                        CloseButtonText = null,
                        DefaultButton = ContentDialogButton.Primary
                    };
                    var showTask = progressDialog.ShowAsync();

                    try
                    {
                        // ����ͼƬ������
                        using (var httpClient = new HttpClient())
                        {
                            var imageData = await httpClient.GetByteArrayAsync(imageUrl);
                            await FileIO.WriteBytesAsync(file, imageData);

                            // ������ɣ����¶Ի���
                            UpdateDialogForCompletion(progressDialog, loader.GetString("Pictures_Download_Dialog_Success"), $"{loader.GetString("Pictures_Download_Dialog_FilePath")}:\n{file.Path}");
                            progressDialog.IsPrimaryButtonEnabled = true;
                            await showTask;
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            // �������ʧ�ܣ�����ɾ���Ѵ������ļ�
                            await file.DeleteAsync();
                        }
                        catch
                        {

                        }
                        // ����ʧ�ܣ����¶Ի���
                        UpdateDialogForCompletion(progressDialog, loader.GetString("Pictures_Download_Dialog_Failure"), $"{loader.GetString("Pictures_Download_Dialog_Error")}:\n{ex.Message}");
                        progressDialog.IsPrimaryButtonEnabled = true;
                        await showTask;
                    }
                }
                button.IsEnabled = true;
            }
        }

        // �������ȶԻ�������
        private StackPanel CreateProgressContent()
        {
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
                    }
                }
            };
        }

        // ���¶Ի�������Ϊ���״̬
        private void UpdateDialogForCompletion(ContentDialog dialog, string title, string message)
        {
            dialog.Title = title;
            dialog.Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.WrapWholeWords
            };
        }

        private string GetCurrentImageUrl()
        {
            return (Picture.Source as BitmapImage)?.UriSource?.ToString() ?? "";
        }
    }
}