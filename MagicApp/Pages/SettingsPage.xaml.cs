using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Windows.Globalization;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        // 初始化标志
        private bool _isInitializing = true;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            // 开始初始化
            _isInitializing = true;

            //设置关于页标题为应用名称
            string appName = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
            About.Header = appName;

            //设置应用版本号
            string Version = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
            AppVersion.Description = Version;

            // 设置关于处超链接文本
            var loader = ResourceLoader.GetForViewIndependentUse();

            string officialWebsiteText = loader.GetString("Settings_About_OfficialWebsite");
            string sourceCodeText = loader.GetString("Settings_About_SourceCode");
            OfficialWebsite.Inlines.Clear();
            OfficialWebsite.Inlines.Add(new Run { Text = officialWebsiteText });
            SourceCode.Inlines.Clear();
            SourceCode.Inlines.Add(new Run { Text = sourceCodeText });

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

            // 设置当前主题选项
            var currentTheme = App.AppTheme;
            if (currentTheme == ElementTheme.Default)
            {
                ThemeModeBox.SelectedItem = Theme_Default;
            }
            else if (currentTheme == ElementTheme.Light)
            {
                ThemeModeBox.SelectedItem = Theme_Light;
            }
            else if (currentTheme == ElementTheme.Dark)
            {
                ThemeModeBox.SelectedItem = Theme_Dark;
            }

            // 初始化完成
            _isInitializing = false;
        }

        //更改主题模式
        private void ThemeMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ThemeSelect = ThemeModeBox.SelectedItem as ComboBoxItem;
            string Theme = ThemeSelect?.Name?.ToString() ?? string.Empty;

            if (Theme == "Theme_Default")
            {
                App.AppTheme = ElementTheme.Default;
            }
            else if (Theme == "Theme_Light")
            {
                App.AppTheme = ElementTheme.Light;
            }
            else if (Theme == "Theme_Dark")
            {
                App.AppTheme = ElementTheme.Dark;
            }
        }

        //更改应用语言
        private async void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果是初始化过程中则不处理
            if (_isInitializing) return;

            var LangSelect = LanguageBox.SelectedItem as ComboBoxItem;
            string Lang = LangSelect?.Name?.ToString() ?? string.Empty;
            string oldLang = ApplicationLanguages.PrimaryLanguageOverride;

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

            // 如果语言确实发生了变化，提示重启
            if (ApplicationLanguages.PrimaryLanguageOverride != oldLang)
            {
                await Task.Delay(100); // 短暂延迟确保语言设置生效
                ShowRestartDialog();
            }
        }

        //显示重启对话框
        private async void ShowRestartDialog()
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = loader.GetString("Settings_Languages_Dialog_Title");
            dialog.Content = loader.GetString("Settings_Languages_Dialog_Content");
            dialog.PrimaryButtonText = loader.GetString("Settings_Languages_Dialog_Restart");
            dialog.CloseButtonText = loader.GetString("Settings_Languages_Dialog_Later");
            dialog.DefaultButton = ContentDialogButton.Primary;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await RestartApplication();
            }
        }

        // 重启应用
        private async Task RestartApplication()
        {
            
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }

        //检查更新
        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdate.IsEnabled = false;
            CheckUpdateProgressRing.IsActive = true;


            using (var httpClient = new HttpClient())
            {
                var loader = ResourceLoader.GetForViewIndependentUse();

                try
                {
                    // 获取当前应用版本
                    var packageVersion = Package.Current.Id.Version;
                    string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";

                    // 设置User-Agent头以符合GitHub API要求
                    string userAgentString = $"MagicApp/{currentVersion}";
                    httpClient.DefaultRequestHeaders.Add("User-Agent", userAgentString);

                    string url = "https://api.github.com/repos/Birdjiujiuuu/MagicApp/releases/latest";
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();

                        using (var json = JsonDocument.Parse(jsonString))
                        {
                            string newestVersion = json.RootElement.GetProperty("tag_name").GetString();

                            // 清理版本号"v"前缀
                            newestVersion = newestVersion.TrimStart('v', 'V');
                            
                            CheckUpdateProgressRing.IsActive = false;

                            // 调试输出，帮助诊断问题
                            Debug.WriteLine($"当前版本: {currentVersion}, 最新版本: {newestVersion}");

                            if (newestVersion == currentVersion)
                            {
                                ContentDialog dialog = new ContentDialog
                                {
                                    XamlRoot = this.XamlRoot,
                                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                                    Title = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Title"),
                                    Content = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Latest"),
                                    CloseButtonText = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Close"),
                                    DefaultButton = ContentDialogButton.Close
                                };
                                var result = await dialog.ShowAsync();
                            }
                            else
                            {
                                // 获取发布说明和下载链接
                                string releaseNotes = json.RootElement.GetProperty("body").GetString();
                                string DownloadUrl = json.RootElement.GetProperty("html_url").GetString();

                                ContentDialog dialog = new ContentDialog
                                {
                                    XamlRoot = this.XamlRoot,
                                    Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                                    Title = newestVersion,
                                    Content = $"{releaseNotes}",
                                    PrimaryButtonText = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Download"),
                                    CloseButtonText = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Later"),
                                    DefaultButton = ContentDialogButton.Primary
                                };
                                var result = await dialog.ShowAsync();

                                if (result == ContentDialogResult.Primary)
                                {
                                    await Windows.System.Launcher.LaunchUriAsync(new Uri(DownloadUrl));
                                }
                            }
                        }
                    }
                    else
                    {
                        // 处理HTTP错误状态
                        CheckUpdateProgressRing.IsActive = false;
                        ContentDialog dialog = new ContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                            Title = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Title"),
                            Content = $"{loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Error")}(HTTP {response.StatusCode})",
                            CloseButtonText = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Close"),
                            DefaultButton = ContentDialogButton.Close
                        };
                        var result = await dialog.ShowAsync();
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    CheckUpdateProgressRing.IsActive = false;
                    ContentDialog dialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Title"),
                        Content = $"{loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Error")}{httpEx.Message}",
                        CloseButtonText = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Close"),
                        DefaultButton = ContentDialogButton.Close
                    };
                    var result = await dialog.ShowAsync();
                }
                catch (JsonException jsonEx)
                {
                    CheckUpdateProgressRing.IsActive = false;
                    ContentDialog dialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Title"),
                        Content = $"{loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Error")}JSON解析失败",
                        CloseButtonText = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Close"),
                        DefaultButton = ContentDialogButton.Close
                    };
                    var result = await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    CheckUpdateProgressRing.IsActive = false;
                    ContentDialog dialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Title"),
                        Content = $"{loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Error")}{ex.Message}",
                        CloseButtonText = loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Close"),
                        DefaultButton = ContentDialogButton.Close
                    };
                    var result = await dialog.ShowAsync();
                }
                finally
                {
                    CheckUpdateProgressRing.IsActive = false;
                    CheckUpdate.IsEnabled = true;
                }
            }
        }
    }
}
