using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Windows.Globalization;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;

namespace MagicApp.Pages
{
    public sealed partial class SettingsPage : Page
    {
        // 初始化标志
        private bool _isInitializing = true;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        private void Page_Loading(FrameworkElement sender, object args)
        {
            // 开始初始化
            _isInitializing = true;

            //设置关于页标题为应用名称
            About.Header = Windows.ApplicationModel.AppInfo.Current.DisplayInfo.DisplayName;

            //设置应用版本号
            AppVersion.Description = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

            // 设置关于处超链接文本
            string officialWebsiteText = _resourceLoader.GetString("Pages_Settings_About_OfficialWebsite");
            string sourceCodeText = _resourceLoader.GetString("Pages_Settings_About_SourceCode");
            OfficialWebsite.Inlines.Clear();
            OfficialWebsite.Inlines.Add(new Run { Text = officialWebsiteText });
            SourceCode.Inlines.Clear();
            SourceCode.Inlines.Add(new Run { Text = sourceCodeText });

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
            else if (currentLang == "ko")
            {
                LanguageBox.SelectedItem = Lang_ko;
            }
            else if (currentLang == "ja")
            {
                LanguageBox.SelectedItem = Lang_ja_jp;
            }
            else if (currentLang == "en-US")
            {
                LanguageBox.SelectedItem = Lang_en_us;
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
            else if (Lang == "Lang_ko")
            {
                ApplicationLanguages.PrimaryLanguageOverride = "ko";
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
            ContentDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = _resourceLoader.GetString("Pages_Settings_Languages_Dialog_Title"),
                Content = _resourceLoader.GetString("Pages_Settings_Languages_Dialog_Content"),
                PrimaryButtonText = _resourceLoader.GetString("Pages_Settings_Languages_Dialog_Restart"),
                CloseButtonText = _resourceLoader.GetString("Pages_Settings_Languages_Dialog_Later"),
                DefaultButton = ContentDialogButton.Primary
            };
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

            // 调用UpdateService
            await UpdateService.CheckForUpdateAsync(
                this.XamlRoot,
                // 检查开始时的回调
                onCheckStart: null,
                // 检查结束时的回调
                onCheckEnd: () =>
                {
                    CheckUpdate.IsEnabled = true;
                    CheckUpdateProgressRing.IsActive = false;
                }
            );
        }
    }
}
