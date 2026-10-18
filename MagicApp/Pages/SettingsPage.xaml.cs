using MagicApp.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.Globalization;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

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
            AppVersion.Text = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

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

            // 加载音效开关状态
            bool soundEnabled = false;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("SoundEnabled", out var soundValue)
                && soundValue is bool saved)
            {
                soundEnabled = saved;
            }
            SoundSwitch.IsOn = soundEnabled;
            ElementSoundPlayer.State = soundEnabled
                ? ElementSoundPlayerState.On
                : ElementSoundPlayerState.Off;

            // 加载空间音效开关状态（仅在音效开启时可用）
            bool spatialAudioEnabled = false;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("SpatialAudioEnabled", out var spatialValue)
                && spatialValue is bool savedSpatial)
            {
                spatialAudioEnabled = savedSpatial;
            }
            SpatialAudioSwitch.IsEnabled = soundEnabled;
            SpatialAudioSwitch.IsOn = soundEnabled && spatialAudioEnabled;
            ElementSoundPlayer.SpatialAudioMode = (soundEnabled && spatialAudioEnabled)
                ? ElementSpatialAudioMode.On
                : ElementSpatialAudioMode.Off;

            // 商店版本禁用检查更新卡片
            if (Package.Current.SignatureKind == PackageSignatureKind.Store)
            {
                Update.Visibility = Visibility.Collapsed;
                CheckUpdate.IsEnabled = false;
            }

            // 加载自动检查更新开关状态
            bool autoCheckUpdatesEnabled = true;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("AutoCheckUpdatesEnabled", out var autoCheckValue)
                && autoCheckValue is bool savedAutoCheck)
            {
                autoCheckUpdatesEnabled = savedAutoCheck;
            }
            AutoCheckUpdatesSwitch.IsOn = autoCheckUpdatesEnabled;

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
                ShowRestartInfoBar();
            }
        }

        // 从底部滑入显示提示条
        private void ShowRestartInfoBar()
        {
            // 每次打开前把 transform 重置回“台下”
            RestartInfoBarTranslate.Y = 100;
            RestartInfoBar.Opacity = 0;
            RestartInfoBar.IsOpen = true;

            var sb = new Storyboard();

            // 上滑：Y 100 → 0
            var slide = new DoubleAnimation
            {
                From = 100,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(slide, RestartInfoBarTranslate);
            Storyboard.SetTargetProperty(slide, "Y");
            sb.Children.Add(slide);

            // 淡入：Opacity 0 → 1
            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(250))
            };
            Storyboard.SetTarget(fade, RestartInfoBar);
            Storyboard.SetTargetProperty(fade, "Opacity");
            sb.Children.Add(fade);

            sb.Begin();
        }

        // 重启应用按钮点击事件
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartInfoBar.IsOpen = false;
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }

        // 音效开关切换事件
        private void SoundSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // 初始化过程中不处理，避免误触发
            if (_isInitializing) return;

            bool isOn = SoundSwitch.IsOn;
            ElementSoundPlayer.State = isOn ? ElementSoundPlayerState.On : ElementSoundPlayerState.Off;

            ApplicationData.Current.LocalSettings.Values["SoundEnabled"] = isOn;

            // 联动空间音效开关
            SpatialAudioSwitch.IsEnabled = isOn;
            if (!isOn)
            {
                SpatialAudioSwitch.IsOn = false;
                ApplicationData.Current.LocalSettings.Values["SpatialAudioEnabled"] = false;
                ElementSoundPlayer.SpatialAudioMode = ElementSpatialAudioMode.Off;
            }
        }

        private void SpatialAudioSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // 初始化过程中不处理，避免误触发
            if (_isInitializing) return;

            // 音效总开关关闭时，不允许空间音效为开
            if (!SoundSwitch.IsOn)
            {
                SpatialAudioSwitch.IsOn = false;
                return;
            }

            bool isOn = SpatialAudioSwitch.IsOn;
            ElementSoundPlayer.SpatialAudioMode = isOn ? ElementSpatialAudioMode.On : ElementSpatialAudioMode.Off;

            ApplicationData.Current.LocalSettings.Values["SpatialAudioEnabled"] = isOn;
        }

        //打开更新记录弹窗
        private async void History_Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Controls.ReleaseHistoryDialog
            {
                RepoOwner = "Birdjiujiuuu",
                RepoName = "MagicApp",
            };

            var popup = new Popup
            {
                XamlRoot = this.XamlRoot,
                Child = dialog,
                HorizontalOffset = 0,
                VerticalOffset = 0,
                IsLightDismissEnabled = false,
            };
            dialog.HostPopup = popup;

            popup.Closed += (_, __) => dialog.Dispose();

            popup.IsOpen = true;
            await dialog.LoadReleasesAsync();
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

        private void AutoCheckUpdatesSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // 初始化过程中不处理，避免误触发
            if (_isInitializing) return;

            ApplicationData.Current.LocalSettings.Values["AutoCheckUpdatesEnabled"] = AutoCheckUpdatesSwitch.IsOn;
        }
    }
}
