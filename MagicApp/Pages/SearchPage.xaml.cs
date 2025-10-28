using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppNotifications.Builder;
using System;

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
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var Engine = ComboBox.SelectedItem as ComboBoxItem;
            string EngineName = Engine?.Name?.ToString() ?? string.Empty;

            if (EngineName == "Google")
            {
                BitmapImage Icons = new BitmapImage();
                Icons.UriSource = new Uri(this.BaseUri, "https://www.google.com/favicon.ico");
                Icon.Source = Icons;
            }
            else if (EngineName == "Bing")
            {
                BitmapImage Icons = new BitmapImage();
                Icons.UriSource = new Uri(this.BaseUri, "https://cn.bing.com/favicon.ico");
                Icon.Source = Icons;
            }
            else if (EngineName == "Baidu")
            {
                BitmapImage Icons = new BitmapImage();
                Icons.UriSource = new Uri(this.BaseUri, "https://www.baidu.com/favicon.ico");
                Icon.Source = Icons;
            }

            this.SearchBox_TextChanged(SearchBox, new AutoSuggestBoxTextChangedEventArgs());
        }


        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var Engine = ComboBox.SelectedItem as ComboBoxItem;
            string EngineName = Engine?.Name?.ToString() ?? string.Empty;

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

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var Engine = ComboBox.SelectedItem as ComboBoxItem;
            string EngineName = Engine?.Name?.ToString() ?? string.Empty;

            if (EngineName == "Google")
            {
                string url = "https://www.google.com/search?q=" + SearchBox.Text;
                System.Diagnostics.Process.Start("explorer.exe", "https://www.google.com/search?q=" + SearchBox.Text);
            }
            else if (EngineName == "Bing")
            {
                string url = "https://www.bing.com/search?q=" + SearchBox.Text;
                System.Diagnostics.Process.Start("explorer.exe", url);
            }
            else if (EngineName == "Baidu")
            {
                string url = "https://www.baidu.com/s?wd=" + SearchBox.Text;
                System.Diagnostics.Process.Start("explorer.exe", url);
            }
        }
    }
}
