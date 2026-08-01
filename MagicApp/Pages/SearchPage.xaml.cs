using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace MagicApp.Pages
{
    public sealed partial class SearchPage : Page
    {
        public SearchPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox.SelectedIndex = 0;

            BitmapImage GoogleIcons = new BitmapImage();
            GoogleIcons.UriSource = new Uri(this.BaseUri, "https://www.google.com/favicon.ico");
            GoogleIcon.Source = GoogleIcons;

            BitmapImage BingIcons = new BitmapImage();
            BingIcons.UriSource = new Uri(this.BaseUri, "https://cn.bing.com/favicon.ico");
            BingIcon.Source = BingIcons;

            BitmapImage BaiduIcons = new BitmapImage();
            BaiduIcons.UriSource = new Uri(this.BaseUri, "https://www.baidu.com/favicon.ico");
            BaiduIcon.Source = BaiduIcons;

            // OpenInBowser 点击事件
            OpenInBowser.Action = async () =>
            {
                try
                {
                    var Engine = ComboBox.SelectedItem as StackPanel;
                    string EngineName = Engine?.Name?.ToString() ?? string.Empty;
                    string url = "";

                    if (EngineName == "Google")
                    {
                        url = "https://www.google.com/search?q=" + SearchBox.Text;
                    }
                    else if (EngineName == "Bing")
                    {
                        url = "https://www.bing.com/search?q=" + SearchBox.Text;
                    }
                    else if (EngineName == "Baidu")
                    {
                        url = "https://www.baidu.com/s?wd=" + SearchBox.Text;
                    }

                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(processStartInfo);
                    return true;
                }
                catch
                {
                    return false;
                }
            };
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SearchBox_TextChanged(SearchBox, new AutoSuggestBoxTextChangedEventArgs());
        }


        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var Engine = ComboBox.SelectedItem as StackPanel;
            string EngineName = Engine?.Name?.ToString() ?? string.Empty;

            if (SearchBox.Text != "")
            {
                if (EngineName == "Google")
                {
                    string url = "https://www.google.com/search?q=" + SearchBox.Text;
                    Uri targetUri = new Uri(url);
                    explorer.Source = targetUri;
                }
                else if (EngineName == "Bing")
                {
                    string url = "https://www.bing.com/search?q=" + SearchBox.Text;
                    Uri targetUri = new Uri(url);
                    explorer.Source = targetUri;
                }
                else if (EngineName == "Baidu")
                {
                    string url = "https://www.baidu.com/s?wd=" + SearchBox.Text;
                    Uri targetUri = new Uri(url);
                    explorer.Source = targetUri;
                }
            }
        }
    }
}
