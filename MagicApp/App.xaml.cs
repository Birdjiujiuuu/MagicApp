using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private Window? _window;

        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        // 主题设置常量
        private const string ThemeSettingKey = "AppTheme";

        // 当前应用主题属性
        public static ElementTheme AppTheme
        {
            get
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values.TryGetValue(ThemeSettingKey, out var themeValue))
                {
                    return (ElementTheme)themeValue;
                }
                return ElementTheme.Default; // 默认跟随系统
            }
            set
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[ThemeSettingKey] = (int)value;
                ApplyTheme(value);
            }
        }

        // 应用主题到所有窗口
        public static void ApplyTheme(ElementTheme theme)
        {
            foreach (var window in Windows)
            {
                if (window.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = theme;
                }
            }
        }

        // 获取所有活动窗口（需要维护窗口列表）
        private static List<Window> Windows { get; } = new List<Window>();

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            Windows.Add(_window);

            MainWindow = _window;

            // 应用保存的主题设置
            if (_window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = AppTheme;
            }

            _window.Activate();
        }

        // 注册窗口（在其他页面创建新窗口时调用）
        public static void RegisterWindow(Window window)
        {
            Windows.Add(window);
            if (window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = AppTheme;
            }
        }

        // 注销窗口（窗口关闭时调用）
        public static void UnregisterWindow(Window window)
        {
            Windows.Remove(window);
        }
    }
}
