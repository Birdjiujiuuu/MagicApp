using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AI.Text;
using Microsoft.Windows.Globalization;
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
using Windows.System.UserProfile;

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
            //设置关于页标题为应用名称
            string appName = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
            About.Header = appName;

            //设置应用版本号
            string Version = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
            AppVersion.Description = Version;

            // 获取当前语言
            string currentLang = ApplicationLanguages.Languages[0];
            if (currentLang == "zh-Hant-MO")
            {
                LanguageBox.SelectedItem = Lang_zh_mo;
            }
            else if (currentLang == "zh-Hans-CN")
            {
                LanguageBox.SelectedItem = Lang_zh_cn;
            }
            else if (currentLang == "zh-Hant-TW")
            {
                LanguageBox.SelectedItem = Lang_zh_tw;
            }
            else if (currentLang == "ja")
            {
                LanguageBox.SelectedItem = Lang_ja_jp;
            }
            else if (currentLang == "en-US")
            {
                LanguageBox.SelectedItem = Lang_en_us;
            }
        }

        //更改主题模式
        private void ThemeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ThemeSelect = ThemeModeBox.SelectedItem as ComboBoxItem;
            string Theme = ThemeSelect?.Name?.ToString() ?? string.Empty;

            if (Theme == "Theme_Default")
            {

            }
            else if (Theme == "Theme_Light")
            {

            }
            else if (Theme == "Theme_Dark")
            {

            }
        }

        //更改应用语言
        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var LangSelect = LanguageBox.SelectedItem as ComboBoxItem;
            string Lang = LangSelect?.Name?.ToString() ?? string.Empty;

            if (Lang == "Lang_zh_mo")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "zh-MO";
            }
            else if (Lang == "Lang_zh_cn")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "zh-CN";
            }
            else if (Lang == "Lang_zh_tw")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "zh-TW";
            }
            else if (Lang == "Lang_ja_jp")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "ja-JP";
            }
            else if (Lang == "Lang_en_us")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "en-US";
            }
        }

        //检查更新
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
