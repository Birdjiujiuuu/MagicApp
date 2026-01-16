using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Drawing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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

        private void OpenInBowser_Click(object sender, RoutedEventArgs e)
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
        }
    }
}
