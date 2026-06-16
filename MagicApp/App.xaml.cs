using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using Windows.Storage;
using WinRT.Interop;

namespace MagicApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private Window? _window;

        public static Window? MainWindow { get; private set; }

        // 创建单实例 GUID
        private const string AppInstanceKey = "MagicApp_9F1C4B8E-3A2D-4F6B-9C8E-7A3D5F1B2C6E";
        private AppInstance? _currentInstance;

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
            // 注册单实例
            _currentInstance = AppInstance.FindOrRegisterForKey(AppInstanceKey);

            // 若已有实例在运行，则激活该实例并传递参数
            if (!_currentInstance.IsCurrent)
            {
                _currentInstance.RedirectActivationToAsync(
                    AppInstance.GetCurrent().GetActivatedEventArgs()
                ).AsTask().Wait();

                Environment.Exit(0);
                return;
            }

            // 创建主窗口
            _currentInstance.Activated += OnAppInstanceActivated;

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

        // 处理单实例激活事件
        private void OnAppInstanceActivated(object? sender, AppActivationArguments e)
        {
            if (_window != null)
            {
                _window.DispatcherQueue.TryEnqueue(() =>
                {
                    var hwnd = WindowNative.GetWindowHandle(_window);
                    var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                    var appWindow = AppWindow.GetFromWindowId(windowId);
                    appWindow.Show(true);  // 强制激活窗口
                    _window.Activate();
                });
            }
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
