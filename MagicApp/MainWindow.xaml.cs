using MagicApp.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Net.Http;
using Windows.Media.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true; // Extend the content into the title bar and hide the default titlebar
            this.SetTitleBar(TitleBar); // Set the custom title bar

            ContentFrame.Navigate(typeof(HomePage), null);
            NavView.Header = home.Content;

            this.MusicRefresh_Click(this, new RoutedEventArgs());
        }

        private void AppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var Info = sender as FrameworkElement;
            FlyoutBase.ShowAttachedFlyout(Info);
        }

        private async void MusicRefresh_Click(object sender, RoutedEventArgs e)
        {
            MusicRefreshFailTip.Text = "";
            MusicRefreshProgressRing.IsActive = true;
            MusicRefresh.IsEnabled = false;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string url = "https://birdjiujiuuu.github.io/magicapp/source/winui3/home/bgm.txt";
                    // 发送GET请求
                    var response = await httpClient.GetAsync(url);

                    // 检查请求是否成功
                    if (response.IsSuccessStatusCode)
                    {
                        string retString = await response.Content.ReadAsStringAsync();
                        int Ida = retString.IndexOf("<id>");
                        int Idb = retString.IndexOf("</id>");
                        string BgmMedia = "https://music.163.com/song/media/outer/url?id=" + retString.Substring(Ida + 4, Idb - Ida - 4) + ".mp3";
                        int BgmNamea = retString.IndexOf("<name>");
                        int BgmNameb = retString.IndexOf("</name>");
                        string BgmNameAB = retString.Substring(BgmNamea + 6, BgmNameb - BgmNamea - 6);
                        int BgmAuthora = retString.IndexOf("<artist>");
                        int BgmAuthorb = retString.IndexOf("</artist>");
                        string BgmAuthorAB = retString.Substring(BgmAuthora + 8, BgmAuthorb - BgmAuthora - 8);
                        int Covera = retString.IndexOf("<cover>");
                        int Coverb = retString.IndexOf("</cover>");
                        string Cover = retString.Substring(Covera + 7, Coverb - Covera - 7);
                        //各种抓取和解析
                        Uri BgmUri = new Uri(BgmMedia);
                        MediaPlayer.Source = MediaSource.CreateFromUri(BgmUri);
                        BitmapImage CoverUri = new BitmapImage();
                        CoverUri.UriSource = new Uri(Cover);
                        CoverImage.Source = CoverUri;
                        MusicName.Text = BgmNameAB;
                        MusicAuthor.Text = BgmAuthorAB;
                    }
                }
                catch
                {
                    MusicRefreshFailTip.Text = "加载失败，请重试！";
                }
                finally
                {
                    MusicRefreshProgressRing.IsActive = false;
                    MusicRefresh.IsEnabled = true;
                }
            }
        }

        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            FrameNavigationOptions navOptions = new FrameNavigationOptions();
            navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;
            if (args.InvokedItemContainer == home)
            {
                ContentFrame.Navigate(typeof(HomePage), null);
                NavView.Header = home.Content;
            }
            else if (args.InvokedItemContainer == Pictures)
            {
                ContentFrame.Navigate(typeof(PicturesPage), null, new DrillInNavigationTransitionInfo());
                NavView.Header = Pictures.Content;
            }
            else if (args.InvokedItemContainer == Music)
            {
                ContentFrame.Navigate(typeof(MusicPage), null, new DrillInNavigationTransitionInfo());
                NavView.Header = Music.Content;
            }
            else if (args.InvokedItemContainer == Search)
            {
                ContentFrame.Navigate(typeof(SearchPage), null, new DrillInNavigationTransitionInfo());
                NavView.Header = Search.Content;
            }
            else if (args.InvokedItemContainer == Notice)
            {
                ContentFrame.Navigate(typeof(NoticePage), null);
                NavView.Header = Notice.Content;
            }
            else if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage), null);
                NavView.Header = "设置";
            }
        }
    }
}
