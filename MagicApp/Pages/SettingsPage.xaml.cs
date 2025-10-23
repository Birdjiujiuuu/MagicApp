using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            string Version = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
            AppVersion.Description = Version;
        }

        private void ColorMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string Color = e.AddedItems[0].ToString();
            if (Color == "使用 Windows 默认")
            {

            }
            else if (Color == "浅色")
            {

            }
            else if (Color == "深色")
            {

            }
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdate.IsEnabled = false;
            CheckUpdateProgressRing.IsActive = true;

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string url = "https://birdjiujiuuu.github.io/magicapp/history/version.html";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string retString = await response.Content.ReadAsStringAsync();
                        int a = retString.IndexOf("winui3:");
                        int b = retString.IndexOf(":winui3");
                        string newestversion = retString.Substring(a + 7, b - a - 7);
                        string version = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

                        CheckUpdateProgressRing.IsActive = false;
                        if (newestversion == version)
                        {
                            ContentDialog dialog = new ContentDialog();
                            dialog.XamlRoot = this.XamlRoot;
                            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                            dialog.Title = "检查更新";
                            dialog.Content = "已经是最新版本！";
                            dialog.CloseButtonText = "确定";
                            dialog.DefaultButton = ContentDialogButton.Close;

                            var result = await dialog.ShowAsync();
                        }
                        else
                        {
                            ContentDialog dialog = new ContentDialog();
                            dialog.XamlRoot = this.XamlRoot;
                            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                            dialog.Title = (newestversion + " 更新！");
                            dialog.Content = "发现新版本,现在可以进行更新！";
                            dialog.PrimaryButtonText = "前往下载";
                            dialog.CloseButtonText = "稍后下载";
                            dialog.DefaultButton = ContentDialogButton.Primary;

                            var result = await dialog.ShowAsync();

                            if (result == ContentDialogResult.Primary)
                            {
                                System.Diagnostics.Process.Start("explorer.exe", "https://github.com/Birdjiujiuuu/MagicApp/releases");
                            }
                            else
                            {
                                // Do nothing.
                            }
                        }
                    }
                }
                catch
                {
                    CheckUpdateProgressRing.IsActive = false;
                    ContentDialog dialog = new ContentDialog();
                    dialog.XamlRoot = this.XamlRoot;
                    dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                    dialog.Title = "检查更新";
                    dialog.Content = "无法连接至服务器，请检查网络后重试";
                    dialog.CloseButtonText = "确定";
                    dialog.DefaultButton = ContentDialogButton.Close;

                    var result = await dialog.ShowAsync();
                }
                finally
                {
                    CheckUpdate.IsEnabled = true;
                }
            }
        }
    }
}
