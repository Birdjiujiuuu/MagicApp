using MagicApp.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MagicApp.Pages
{
    public sealed partial class SystemInfoPage : Page
    {
        // 用于内存信息获取的 Win32 API
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        // 用于获取主屏幕分辨率
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        public SystemInfoPage()
        {
            InitializeComponent();
            this.Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            LoadSystemInfo();
        }

        private void LoadSystemInfo()
        {
            try
            {
                // ========== 设备规格 ==========
                // 处理器
                txtCpu.Text = GetProcessorName();

                // 内存
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    ulong totalBytes = memStatus.ullTotalPhys;
                    ulong availBytes = memStatus.ullAvailPhys;
                    ulong usedBytes = totalBytes - availBytes;
                    double percent = totalBytes > 0 ? (double)usedBytes / totalBytes * 100.0 : 0;

                    txtMemoryUsed.Text = FormatHelper.FormatFileSize((long)usedBytes, 1);
                    txtMemoryTotal.Text = $"共 {FormatHelper.FormatFileSize((long)totalBytes, 1)}";
                    memoryBar.Value = percent;
                }

                // 驱动器信息（排除光盘等不可用驱动器）
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => new
                    {
                        Name = d.Name.TrimEnd('\\'),
                        TotalBytes = d.TotalSize,
                        FreeBytes = d.AvailableFreeSpace,
                        UsedBytes = d.TotalSize - d.AvailableFreeSpace,
                        TotalSpaceStr = $"共 {FormatHelper.FormatFileSize(d.TotalSize, 1)}",
                        FreeSpaceStr = $"可用 {FormatHelper.FormatFileSize(d.AvailableFreeSpace, 1)}"
                    }).ToList();

                drivesList.ItemsSource = drives;

                // 显卡（主显卡）
                txtGpu.Text = GetAllGpuNames();

                // 屏幕分辨率
                int width = GetSystemMetrics(SM_CXSCREEN);
                int height = GetSystemMetrics(SM_CYSCREEN);
                txtResolution.Text = $"{width} × {height}";

                // 系统架构
                txtArchitecture.Text = RuntimeInformation.OSArchitecture.ToString();

                // ========== Windows 规格 ==========
                // 从注册表获取详细信息
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        string productName = key.GetValue("ProductName")?.ToString() ?? "未知";
                        string displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? "";
                        string currentBuild = key.GetValue("CurrentBuild")?.ToString() ?? "";
                        string ubr = key.GetValue("UBR")?.ToString() ?? "";
                        string buildLabEx = key.GetValue("BuildLabEx")?.ToString() ?? "";

                        // 处理 Windows 10/11 命名不一致问题
                        if (int.TryParse(currentBuild, out int buildNumber) && buildNumber >= 22000)
                        {
                            // 如果当前构建号 >= 22000（Windows 11），但名称里包含 Windows 10，则替换
                            if (productName.Contains("Windows 10"))
                            {
                                productName = productName.Replace("Windows 10", "Windows 11");
                            }
                            // 如果连 Windows 都没有（某些精简版），则根据 EditionID 重新生成
                            if (!productName.Contains("Windows"))
                            {
                                string editionID = key.GetValue("EditionID")?.ToString() ?? "";
                                productName = $"Windows 11 {editionID}";
                            }
                        }

                        // 版本信息组合
                        txtEdition.Text = productName;
                        txtVersionDisplay.Text = string.IsNullOrEmpty(displayVersion) ? "未知" : displayVersion;
                        txtBuild.Text = $"Build {currentBuild}.{ubr}";
                        txtExperience.Text = string.IsNullOrEmpty(buildLabEx) ? "未知" : buildLabEx.Split('.').LastOrDefault();

                        // 安装日期
                        if (key.GetValue("InstallDate") is int installDateUnix)
                        {
                            DateTime installDate = FormatHelper.UnixTimeStampToDateTime(installDateUnix);
                            txtInstallDate.Text = FormatHelper.FormatDateTime(installDate, "yyyy-MM-dd");
                        }
                        else
                        {
                            txtInstallDate.Text = "未知";
                        }
                    }
                }

                // ========== 运行时 ==========
                txtDotNet.Text = RuntimeInformation.FrameworkDescription;

                // 系统启动时长
                long uptimeMillis = Environment.TickCount64;
                TimeSpan uptime = TimeSpan.FromMilliseconds(uptimeMillis);
                txtUptime.Text = $"{uptime.Days} 天 {uptime.Hours} 小时 {uptime.Minutes} 分钟";
            }
            catch (Exception ex)
            {
                // 忽略异常，保持界面默认值
                Debug.WriteLine($"获取系统信息异常: {ex.Message}");
            }
        }

        private string GetProcessorName()
        {
            try
            {
                // 显式声明为可空对象
                object? value = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "ProcessorNameString",
                    Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "未知处理器"
                );

                return value?.ToString() ?? "未知处理器";
            }
            catch
            {
                return "未知处理器";
            }
        }

        private string GetAllGpuNames()
        {
            var gpuList = new System.Collections.Generic.List<string>();
            string basePath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

            try
            {
                using (var baseKey = Registry.LocalMachine.OpenSubKey(basePath))
                {
                    if (baseKey != null)
                    {
                        foreach (string subKeyName in baseKey.GetSubKeyNames())
                        {
                            using (var subKey = baseKey.OpenSubKey(subKeyName))
                            {
                                string? driverDesc = subKey?.GetValue("DriverDesc") as string;

                                if (driverDesc is string desc &&
                                    !string.IsNullOrEmpty(desc) &&
                                    !desc.Contains("Microsoft Basic Render Driver"))
                                {
                                    gpuList.Add(desc);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 去重后返回（有些键可能重复描述）
            var distinctGpus = gpuList.Distinct().ToList();
            return distinctGpus.Count > 0
                ? string.Join(Environment.NewLine, distinctGpus)
                : "未知显卡";
        }
    }
}