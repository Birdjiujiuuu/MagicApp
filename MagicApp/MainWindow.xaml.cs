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
using Windows.ApplicationModel.Resources;
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

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(TitleBar);

            //设置标题栏标题为应用名称
            string appName = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
            TitleBar.Title = appName;

            // 获取当前窗口的 AppWindow 实例
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            // 设置窗口最小尺寸
            appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
            var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(true, true);
                presenter.PreferredMinimumWidth = 1280;
                presenter.PreferredMinimumHeight = 720;
            }

            // 获取屏幕中心点并移动窗口
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            var centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            var centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));

            NavView.SelectedItem = Home;
            ContentFrame.Navigate(typeof(HomePage), null);
            NavView.Header = Home.Content;

            this.MusicRefresh_Click(this, new RoutedEventArgs());
        }

        // 音乐 AppBarButton 点击事件，显示附加的 Flyout
        private void AppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var Info = sender as FrameworkElement;
            FlyoutBase.ShowAttachedFlyout(Info);
        }

        // 音乐刷新按钮点击事件
        private async void MusicRefresh_Click(object sender, RoutedEventArgs e)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            MusicRefreshFailTip.Text = "";
            MusicRefreshProgressRing.IsActive = true;
            MusicRefresh.IsEnabled = false;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string url = "https://birdjiujiuuu.github.io/magicapp/source/winui3/home/bgm.txt";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        // 解析返回的 XML 数据
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
                        Uri BgmUri = new Uri(BgmMedia);

                        // 设置 MediaPlayer 播放音乐
                        MediaPlayer.Source = MediaSource.CreateFromUri(BgmUri);
                        BitmapImage CoverUri = new BitmapImage();
                        CoverUri.UriSource = new Uri(Cover);
                        CoverImage.Source = CoverUri;
                        MusicName.Text = BgmNameAB;
                        MusicAuthor.Text = BgmAuthorAB;

                        MediaPlayer.AutoPlay = true;
                    }
                }
                catch
                {
                    MusicRefreshFailTip.Text = loader.GetString("Main_Music_Load_Failure");
                }
                finally
                {
                    MusicRefreshProgressRing.IsActive = false;
                    MusicRefresh.IsEnabled = true;
                }
            }
        }

        // 标题栏面板切换请求事件
        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        // 导航视图项目调用事件
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            FrameNavigationOptions navOptions = new FrameNavigationOptions();
            navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;
            if (args.InvokedItemContainer == Home)
            {
                ContentFrame.Navigate(typeof(HomePage), null);
                NavView.Header = Home.Content;
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
                NavView.Header = loader.GetString("Main_NavView_Settings");
            }
        }
    }
}
