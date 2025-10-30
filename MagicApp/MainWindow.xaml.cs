using MagicApp.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;

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

            //���ñ����������ͼ��
            string appName = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
            TitleBar.Title = appName;
            AppWindow.Title = appName;
            AppWindow.SetTitleBarIcon("Assets/MagicAppIcon.ico");
            AppWindow.SetTaskbarIcon("Assets/MagicAppIcon.ico");

            // ��ȡ��ǰ���ڵ� AppWindow ʵ��
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            // ���ô�����С�ߴ�
            appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
            var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(true, true);
                presenter.PreferredMinimumWidth = 1280;
                presenter.PreferredMinimumHeight = 720;
            }

            // ��ȡ��Ļ���ĵ㲢�ƶ�����
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            var centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            var centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));

            NavView.SelectedItem = Home;
            ContentFrame.Navigate(typeof(HomePage), null);
            NavView.Header = Home.Content;

            this.MusicRefresh_Click(this, new RoutedEventArgs());
        }

        // ���� AppBarButton ����¼�����ʾ���ӵ� Flyout
        private void AppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var Info = sender as FrameworkElement;
            FlyoutBase.ShowAttachedFlyout(Info);
        }

        // ����ˢ�°�ť����¼�
        private async void MusicRefresh_Click(object sender, RoutedEventArgs e)
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            MusicRefreshFailTip.Text = "";
            MusicRefreshProgressRing.IsActive = true;
            MusicRefresh.IsEnabled = false;
            MediaPlayer.IsEnabled = false;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string url = "https://birdjiujiuuu.github.io/magicapp/source/winui3/home/bgm.xml";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string retString = await response.Content.ReadAsStringAsync();

                        var doc = XDocument.Parse(retString);
                        var musicElement = doc.Descendants("music").FirstOrDefault();

                        if (musicElement != null)
                        {

                            string BgmMedia = "https://music.163.com/song/media/outer/url?id=" + musicElement.Element("id")?.Value + ".mp3";
                            string? BgmName = musicElement.Element("name")?.Value;
                            string? BgmAuthor = musicElement.Element("artist")?.Value;
                            string? Cover = musicElement.Element("cover")?.Value;

                            // ���� MediaPlayer ��������
                            Uri BgmUri = new Uri(BgmMedia);
                            MediaPlayer.Source = MediaSource.CreateFromUri(BgmUri);
                            MusicName.Text = BgmName;
                            MusicAuthor.Text = BgmAuthor;

                            // ���÷���ͼƬ
                            if (string.IsNullOrEmpty(Cover))
                            {
                                CoverImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/None.png"));
                            }
                            else
                            {
                                BitmapImage CoverUri = new BitmapImage();
                                CoverUri.UriSource = new Uri(Cover);
                                CoverImage.Source = CoverUri;
                            }

                            MediaPlayer.IsEnabled = true;
                        }
                    }
                    else
                    {
                        CoverImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/None.png"));
                        MusicName.Text = loader.GetString("Main_MusicName");
                        MusicAuthor.Text = "-";
                        MusicRefreshFailTip.Text = loader.GetString("Main_Music_Load_Get_Error");
                    }
                }
                catch
                {
                    CoverImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/None.png"));
                    MusicName.Text = loader.GetString("Main_MusicName");
                    MusicAuthor.Text = "-";
                    MusicRefreshFailTip.Text = loader.GetString("Main_Music_Load_Connect_Error");
                }
                finally
                {
                    MusicRefreshProgressRing.IsActive = false;
                    MusicRefresh.IsEnabled = true;
                }
            }
        }

        // ����������л������¼�
        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            NavView.IsPaneOpen = !NavView.IsPaneOpen;
        }

        // ������ͼ��Ŀ�����¼�
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
