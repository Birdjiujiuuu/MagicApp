using MagicApp.Helpers;
using MagicApp.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.ApplicationModel.Resources;

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

        private DispatcherTimer? _timer;

        public SystemInfoPage()
        {
            InitializeComponent();
            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        private static readonly ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            LoadSystemInfo();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, args) => UpdateDynamicInfo();
            _timer.Start();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            _timer = null;
        }

        private void LoadSystemInfo()
        {
            try
            {
                // ========== 设备规格 ==========
                // 设备名称
                txtDeviceName.Text = Environment.MachineName;

                // 主板
                txtMotherboard.Text = GetMotherboardInfo();

                // BIOS 版本
                txtBiosVersion.Text = GetBiosVersion();

                // 系统型号
                txtSystemModel.Text = GetSystemModel();

                // 处理器
                txtCpu.Text = GetProcessorName();

                // 内存
                UpdateMemoryInfo();

                // 虚拟内存
                UpdatePageFileInfo();

                // 驱动器
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d =>
                    {
                        long total = d.TotalSize;
                        long free = d.AvailableFreeSpace;
                        long used = total - free;
                        double percent = total > 0 ? (double)used / total * 100.0 : 0;
                        return new SystemInfoDriveItem
                        {
                            Name = d.Name.TrimEnd('\\'),
                            TotalBytes = (double)total,
                            FreeBytes = (double)free,
                            UsedBytes = (double)used,
                            TotalSpaceStr = FormatHelper.FormatFileSize(total, 1),
                            FreeSpaceStr = _resourceLoader.GetString("SystemInfo_Available") + $" {FormatHelper.FormatFileSize(free, 1)}",
                            UsagePercent = percent,
                            IsHighUsage = percent > 90
                        };
                    }).ToList();

                drivesList.ItemsSource = drives;

                // 显卡
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
                        string productName = key.GetValue("ProductName")?.ToString() ?? _resourceLoader.GetString("SystemInfo_Unknow");
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
                            // 如果连 Windows 都没有，则根据 EditionID 重新生成
                            if (!productName.Contains("Windows"))
                            {
                                string editionID = key.GetValue("EditionID")?.ToString() ?? "";
                                productName = $"Windows 11 {editionID}";
                            }
                        }

                        // 版本信息组合
                        txtEdition.Text = productName;
                        txtVersionDisplay.Text = string.IsNullOrEmpty(displayVersion) ? _resourceLoader.GetString("SystemInfo_Unknow") : displayVersion;
                        txtBuild.Text = $"{currentBuild}.{ubr}";
                        txtExperience.Text = string.IsNullOrEmpty(buildLabEx) ? _resourceLoader.GetString("SystemInfo_Unknow") : buildLabEx.Split('.').LastOrDefault();

                        // 安装日期
                        if (key.GetValue("InstallDate") is int installDateUnix)
                        {
                            DateTime installDate = FormatHelper.UnixTimeStampToDateTime(installDateUnix);
                            txtInstallDate.Text = FormatHelper.FormatDateTime(installDate, "yyyy/MM/dd");
                        }
                        else
                        {
                            txtInstallDate.Text = _resourceLoader.GetString("SystemInfo_Unknow");
                        }
                    }
                }

                // ========== 网络 ==========
                var networkInfo = GetNetworkInfo();
                // 网络适配器
                txtAdapter.Text = networkInfo.AdapterName;

                // 连接状态
                txtNetworkStatus.Text = networkInfo.Status;

                // IPv4 地址
                txtIpv4Address.Text = networkInfo.Ipv4Address;

                // IPv6 地址
                txtIpv6Address.Text = networkInfo.Ipv6Address;

                // MAC 地址
                txtMacAddress.Text = networkInfo.MacAddress;

                // 默认网关
                txtGateway.Text = networkInfo.Gateway;

                // DNS 服务器
                txtDnsServers.Text = networkInfo.DnsServers;

                // ========== 运行时 ==========
                // .NET 版本
                txtDotNet.Text = RuntimeInformation.FrameworkDescription;

                // 系统启动时长
                UpdateUptimeInfo();

                // 当前用户
                txtCurrentUser.Text = Environment.UserName;

                // 用户目录
                txtUserProfile.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            catch
            {

            }
        }

        private string GetMotherboardInfo()
        {
            try
            {
                string? manufacturer = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS",
                    "BaseBoardManufacturer", null) as string;
                string? product = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS",
                    "BaseBoardProduct", null) as string;

                if (!string.IsNullOrWhiteSpace(manufacturer) || !string.IsNullOrWhiteSpace(product))
                {
                    if (!string.IsNullOrWhiteSpace(manufacturer) && !string.IsNullOrWhiteSpace(product))
                        return $"{manufacturer} {product}";
                    return manufacturer ?? product ?? _resourceLoader.GetString("SystemInfo_Unknow");
                }
                return _resourceLoader.GetString("SystemInfo_Unknow");
            }
            catch
            {
                return _resourceLoader.GetString("SystemInfo_Unknow");
            }
        }

        private string GetBiosVersion()
        {
            try
            {
                string? biosVersion = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS",
                    "BIOSVersion", null) as string;
                return !string.IsNullOrWhiteSpace(biosVersion) ? biosVersion : _resourceLoader.GetString("SystemInfo_Unknow");
            }
            catch { return _resourceLoader.GetString("SystemInfo_Unknow"); }
        }

        private string GetSystemModel()
        {
            try
            {
                string? manufacturer = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS",
                    "SystemManufacturer", null) as string;
                string? productName = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS",
                    "SystemProductName", null) as string;
                if (!string.IsNullOrWhiteSpace(manufacturer) || !string.IsNullOrWhiteSpace(productName))
                    return $"{manufacturer} {productName}".Trim();
                return _resourceLoader.GetString("SystemInfo_Unknow");
            }
            catch { return _resourceLoader.GetString("SystemInfo_Unknow"); }
        }

        private string GetProcessorName()
        {
            try
            {
                object? value = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                    "ProcessorNameString",
                    Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? _resourceLoader.GetString("SystemInfo_Unknow")
                );

                return value?.ToString() ?? _resourceLoader.GetString("SystemInfo_Unknow");
            }
            catch
            {
                return _resourceLoader.GetString("SystemInfo_Unknow");
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
                : _resourceLoader.GetString("SystemInfo_Unknow");
        }

        private (string AdapterName, string Status, string Ipv4Address, string Ipv6Address, string MacAddress, string Gateway, string DnsServers) GetNetworkInfo()
        {
            string adapter = _resourceLoader.GetString("SystemInfo_Unknow");
            string status = _resourceLoader.GetString("SystemInfo_Unknow");
            string ipv4 = _resourceLoader.GetString("SystemInfo_NoConnection");
            string ipv6 = _resourceLoader.GetString("SystemInfo_NoConnection");
            string mac = _resourceLoader.GetString("SystemInfo_Unknow");
            string gateway = _resourceLoader.GetString("SystemInfo_Unknow");
            string dns = _resourceLoader.GetString("SystemInfo_Unknow");

            try
            {
                var nic = System.Net.NetworkInformation.NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                             && n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                             && n.GetIPProperties().UnicastAddresses.Any(addr =>
                                 addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ||
                                 addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
                    .FirstOrDefault();

                if (nic != null)
                {
                    adapter = nic.Name;
                    status = nic.OperationalStatus.ToString();
                    mac = string.Join(":", nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));

                    var ipProps = nic.GetIPProperties();
                    var ipv4Addr = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    var ipv6Addr = ipProps.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);

                    ipv4 = ipv4Addr?.Address.ToString() ?? _resourceLoader.GetString("SystemInfo_NoConnection");
                    ipv6 = ipv6Addr?.Address.ToString() ?? _resourceLoader.GetString("SystemInfo_NoConnection");

                    // 默认网关（取 IPv4 网关）
                    var gw = ipProps.GatewayAddresses
                        .FirstOrDefault(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    gateway = gw?.Address.ToString() ?? _resourceLoader.GetString("SystemInfo_Unknow");

                    // DNS 服务器（所有地址）
                    var dnsAddresses = ipProps.DnsAddresses;
                    dns = dnsAddresses.Any() ? string.Join(", ", dnsAddresses) : _resourceLoader.GetString("SystemInfo_Unknow");
                }
            }
            catch { }

            return (adapter, status, ipv4, ipv6, mac, gateway, dns);
        }

        // 更新动态信息
        private void UpdateDynamicInfo()
        {
            UpdateMemoryInfo();
            UpdateUptimeInfo();
            UpdatePageFileInfo();
        }

        private void UpdateMemoryInfo()
        {
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    ulong totalBytes = memStatus.ullTotalPhys;
                    ulong availBytes = memStatus.ullAvailPhys;
                    ulong usedBytes = totalBytes - availBytes;
                    double percent = totalBytes > 0 ? (double)usedBytes / totalBytes * 100.0 : 0;

                    txtMemoryUsed.Text = _resourceLoader.GetString("SystemInfo_Used") + $" {FormatHelper.FormatFileSize((long)usedBytes, 1)}";
                    txtMemoryTotal.Text = FormatHelper.FormatFileSize((long)totalBytes, 1);
                    memoryBar.Value = percent;
                    if (percent > 90)
                    {
                        memoryBar.ShowError = true;
                    }
                    else
                    {
                        memoryBar.ShowError = false;
                    }
                }
            }
            catch { }
        }

        private void UpdatePageFileInfo()
        {
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    ulong totalPage = memStatus.ullTotalPageFile;
                    ulong availPage = memStatus.ullAvailPageFile;
                    ulong usedPage = totalPage - availPage;
                    double percent = totalPage > 0 ? (double)usedPage / totalPage * 100.0 : 0;

                    txtPageUsed.Text = _resourceLoader.GetString("SystemInfo_Used") + $" {FormatHelper.FormatFileSize((long)usedPage, 1)}";
                    txtPageTotal.Text = FormatHelper.FormatFileSize((long)totalPage, 1);
                    pageBar.Value = percent;
                    if (percent > 90)
                    {
                        pageBar.ShowError = true;
                    }
                    else
                    {
                        pageBar.ShowError = false;
                    }
                }
            }
            catch { }
        }

        private void UpdateUptimeInfo()
        {
            try
            {
                long uptimeMillis = Environment.TickCount64;
                TimeSpan uptime = TimeSpan.FromMilliseconds(uptimeMillis);
                txtUptime.Text = $"{uptime.Days}:{uptime.Hours}:{uptime.Minutes}:{uptime.Seconds}";
            }
            catch { }
        }
    }
}