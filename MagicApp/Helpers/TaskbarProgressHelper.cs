using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MagicApp.Helpers
{
    public enum TaskbarProgressState
    {
        NoProgress = 0x0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }

    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITaskbarList3
    {
        [PreserveSig] int HrInit();
        [PreserveSig] int AddTab(IntPtr hwnd);
        [PreserveSig] int DeleteTab(IntPtr hwnd);
        [PreserveSig] int ActivateTab(IntPtr hwnd);
        [PreserveSig] int SetActiveAlt(IntPtr hwnd);
        [PreserveSig] int MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
        [PreserveSig] int SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        [PreserveSig] int SetProgressState(IntPtr hwnd, TaskbarProgressState tbpFlags);
        [PreserveSig] int RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
        [PreserveSig] int UnregisterTab(IntPtr hwndTab);
        [PreserveSig] int SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
        [PreserveSig] int SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
        [PreserveSig] int ThumbBarAddButtons(IntPtr hwnd, uint cButtons, IntPtr pButtons);
        [PreserveSig] int ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, IntPtr pButtons);
        [PreserveSig] int ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
        [PreserveSig] int SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
        [PreserveSig] int SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
        [PreserveSig] int SetThumbnailClip(IntPtr hwnd, IntPtr prcClip);
    }

    [ComImport]
    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterface(ClassInterfaceType.None)]
    internal class TaskbarList { }

    // 任务栏进度辅助类。
    public static class TaskbarProgressHelper
    {
        private static ITaskbarList3? _taskbarList;
        private static IntPtr _hwnd = IntPtr.Zero;

        // 显式绑定窗口句柄
        public static void Attach(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _taskbarList = null; // 重新创建
            EnsureInitialized();
        }

        private static bool EnsureInitialized()
        {
            if (_taskbarList != null && _hwnd != IntPtr.Zero)
                return true;

            try
            {
                if (_hwnd == IntPtr.Zero)
                {
                    var window = App.MainWindow;
                    if (window == null)
                    {
                        Debug.WriteLine("[Taskbar] App.MainWindow == null");
                        return false;
                    }
                    _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                }

                if (_hwnd == IntPtr.Zero)
                {
                    Debug.WriteLine("[Taskbar] HWND == 0");
                    return false;
                }

                var list = (ITaskbarList3)new TaskbarList();
                int hr = list.HrInit();
                if (hr != 0)
                {
                    Debug.WriteLine($"[Taskbar] HrInit failed: 0x{hr:X8}");
                    return false;
                }

                _taskbarList = list;
                Debug.WriteLine($"[Taskbar] Init OK, HWND=0x{_hwnd.ToInt64():X}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Taskbar] Init exception: {ex}");
                return false;
            }
        }

        public static void SetProgress(ulong completed, ulong total)
        {
            if (total == 0) return;
            if (!EnsureInitialized()) return;
            try
            {
                int hr1 = _taskbarList!.SetProgressState(_hwnd, TaskbarProgressState.Normal);
                int hr2 = _taskbarList!.SetProgressValue(_hwnd, completed, total);
                Debug.WriteLine($"[Taskbar] SetProgress {completed}/{total} state=0x{hr1:X8} value=0x{hr2:X8}");
            }
            catch (Exception ex) { Debug.WriteLine($"[Taskbar] SetProgress ex: {ex.Message}"); }
        }

        public static void SetNormal()
        {
            if (!EnsureInitialized()) return;
            try { _taskbarList!.SetProgressState(_hwnd, TaskbarProgressState.Normal); } catch { }
        }

        public static void SetIndeterminate()
        {
            if (!EnsureInitialized()) return;
            try { _taskbarList!.SetProgressState(_hwnd, TaskbarProgressState.Indeterminate); } catch { }
        }

        public static void SetError()
        {
            if (!EnsureInitialized()) return;
            try { _taskbarList!.SetProgressState(_hwnd, TaskbarProgressState.Error); } catch { }
        }

        public static void Clear()
        {
            if (!EnsureInitialized()) return;
            try { _taskbarList!.SetProgressState(_hwnd, TaskbarProgressState.NoProgress); } catch { }
        }
    }
}