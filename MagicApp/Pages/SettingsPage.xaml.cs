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
        // ��ʼ����־
        private bool _isInitializing = true;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Loading(FrameworkElement sender, object args)
        {
            // ��ʼ��ʼ��
            _isInitializing = true;

            //���ù���ҳ����ΪӦ������
            string appName = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;
            About.Header = appName;

            //����Ӧ�ð汾��
            string Version = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
            AppVersion.Description = Version;

            // ���ù��ڴ��������ı�
            var loader = ResourceLoader.GetForViewIndependentUse();

            string officialWebsiteText = loader.GetString("Settings_About_OfficialWebsite");
            string sourceCodeText = loader.GetString("Settings_About_SourceCode");
            OfficialWebsite.Inlines.Clear();
            OfficialWebsite.Inlines.Add(new Run { Text = officialWebsiteText });
            SourceCode.Inlines.Clear();
            SourceCode.Inlines.Add(new Run { Text = sourceCodeText });

            // ��ȡ��ǰ����
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

            // ���õ�ǰ����ѡ��
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

            // ��ʼ�����
            _isInitializing = false;
        }

        //��������ģʽ
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

        //����Ӧ������
        private async void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ����ǳ�ʼ���������򲻴���
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

            // �������ȷʵ�����˱仯����ʾ����
            if (ApplicationLanguages.PrimaryLanguageOverride != oldLang)
            {
                await Task.Delay(100); // �����ӳ�ȷ������������Ч
                ShowRestartDialog();
            }
        }

        //��ʾ�����Ի���
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

        // ����Ӧ��
        private async Task RestartApplication()
        {
            
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }

        //������
        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdate.IsEnabled = false;
            CheckUpdateProgressRing.IsActive = true;


            using (var httpClient = new HttpClient())
            {
                var loader = ResourceLoader.GetForViewIndependentUse();

                try
                {
                    // ��ȡ��ǰӦ�ð汾
                    var packageVersion = Package.Current.Id.Version;
                    string currentVersion = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";

                    // ����User-Agentͷ�Է���GitHub APIҪ��
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

                            // ����汾��"v"ǰ׺
                            newestVersion = newestVersion.TrimStart('v', 'V');
                            
                            CheckUpdateProgressRing.IsActive = false;

                            // ��������������������
                            Debug.WriteLine($"��ǰ�汾: {currentVersion}, ���°汾: {newestVersion}");

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
                                // ��ȡ����˵������������
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
                        // ����HTTP����״̬
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
                        Content = $"{loader.GetString("Settings_AppVersion_CheckUpdate_Dialog_Error")}JSON����ʧ��",
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
