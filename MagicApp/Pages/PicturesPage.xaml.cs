using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

        private void Page_Loading(FrameworkElement sender, object args)
        {
            ComboBox.SelectedValue = "Bing";
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string SourceWeb = e.AddedItems[0].ToString();
            if (SourceWeb == "Bing")
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        string url = "https://cn.bing.com/HPImageArchive.aspx?idx=0&n=1";
                        var response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string retString = await response.Content.ReadAsStringAsync();
                            int a = retString.IndexOf("<url>");
                            int b = retString.IndexOf("</url>");
                            string imgurl = "http://cn.bing.com" + retString.Substring(a + 5, b - a - 5);
                            int InfoTitlea = retString.IndexOf("<headline>");
                            int InfoTitleb = retString.IndexOf("</headline>");
                            string InfoTitleab = retString.Substring(InfoTitlea + 10, InfoTitleb - InfoTitlea - 10);
                            int InfoBodya = retString.IndexOf("<copyright>");
                            int InfoBodyb = retString.IndexOf("</copyright>");
                            string InfoBodyab = retString.Substring(InfoBodya + 11, InfoBodyb - InfoBodya - 11);
                            BitmapImage Pictures = new BitmapImage();
                            Pictures.UriSource = new Uri(this.BaseUri, imgurl);
                            Picture.Source = Pictures;
                            BitmapImage Icons = new BitmapImage();
                            Icons.UriSource = new Uri(this.BaseUri, "https://cn.bing.com/favicon.ico");
                            Icon.Source = Icons;
                            InfoTitle.Text = InfoTitleab;
                            InfoBody.Text = InfoBodyab;
                        }
                    }
                    catch
                    {
                        InfoTitle.Text = "错误";
                        InfoBody.Text = "无法获取Bing每日一图，请检查网络连接";
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
                            int a = retString.IndexOf("\"hdurl\":\"");
                            int b = retString.IndexOf("\",\"media_type");
                            string imgurl = retString.Substring(a + 9, b - a - 9);
                            int InfoTitlea = retString.IndexOf("\"title\":\"");
                            int InfoTitleb = retString.IndexOf("\",\"url");
                            string InfoTitleab = retString.Substring(InfoTitlea + 9, InfoTitleb - InfoTitlea - 9);
                            int InfoBodya = retString.IndexOf("\"explanation\":\"");
                            int InfoBodyb = retString.IndexOf("\",\"hdurl");
                            string InfoBodyab = retString.Substring(InfoBodya + 15, InfoBodyb - InfoBodya - 15);
                            BitmapImage Pictures = new BitmapImage();
                            Pictures.UriSource = new Uri(this.BaseUri, imgurl);
                            Picture.Source = Pictures;
                            BitmapImage Icons = new BitmapImage();
                            Icons.UriSource = new Uri(this.BaseUri, "https://apod.nasa.gov/favicon.ico");
                            Icon.Source = Icons;
                            InfoTitle.Text = InfoTitleab;
                            InfoBody.Text = InfoBodyab;
                        }
                    }
                    catch
                    {
                        InfoTitle.Text = "错误";
                        InfoBody.Text = "无法获取NASA每日一图，请检查网络连接";
                    }
                }
            }
        }
    }
}
