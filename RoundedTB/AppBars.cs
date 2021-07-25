using System;
using System.Runtime.InteropServices;

namespace RoundedTB
{
    class AppBars
    {
        [DllImport("shell32.dll")]
        public static extern IntPtr SHAppBarMessage(UInt32 dwMessage, ref APPBARDATA pData);

        public enum AppBarMessages
        {
            New = 0x00,
            Remove = 0x01,
            QueryPos = 0x02,
            SetPos = 0x03,
            GetState = 0x04,
            GetTaskBarPos = 0x05,
            Activate = 0x06,
            GetAutoHideBar = 0x07,
            SetAutoHideBar = 0x08,
            WindowPosChanged = 0x09,
            SetState = 0x0a
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public UInt32 cbSize;
            public IntPtr hWnd;
            public UInt32 uCallbackMessage;
            public UInt32 uEdge;
            public LocalPInvoke.RECT rc;
            public Int32 lParam;
        }

        public enum AppBarStates
        {
            AutoHide = 0x01,
            AlwaysOnTop = 0x02
        }

        /// <summary>
        /// Set the Taskbar State option
        /// </summary>
        /// <param name="option">AppBarState to activate</param>
        public static void SetAppbarState(IntPtr hwnd, AppBarStates option)
        {
            APPBARDATA msgData = new APPBARDATA();
            msgData.cbSize = (UInt32)Marshal.SizeOf(msgData);
            msgData.hWnd = hwnd;
            msgData.lParam = (Int32)(option);
            SHAppBarMessage((UInt32)AppBarMessages.SetState, ref msgData);
        }

        /// <summary>
        /// Gets the current Taskbar state
        /// </summary>
        /// <returns>current Taskbar state</returns>
        public static AppBarStates GetAppbarState(IntPtr hwnd)
        {
            APPBARDATA msgData = new APPBARDATA();
            msgData.cbSize = (UInt32)Marshal.SizeOf(msgData);
            msgData.hWnd = hwnd;
            return (AppBarStates)SHAppBarMessage((UInt32)AppBarMessages.GetState, ref msgData);
        }

        /// <summary>
        /// Unregisters the taskbar as an appbar
        /// </summary>
        public static void MakeAppbarSad(IntPtr hwnd)
        {
            APPBARDATA msgData = new APPBARDATA();
            msgData.cbSize = (UInt32)Marshal.SizeOf(msgData);
            msgData.hWnd = hwnd;
            SHAppBarMessage((UInt32)AppBarMessages.Remove, ref msgData);
        }

        /// <summary>
        /// Unregisters the taskbar as an appbar
        /// </summary>
        public static void SetAppbarRect(IntPtr hwnd, LocalPInvoke.RECT rc)
        {
            APPBARDATA msgData = new APPBARDATA();
            msgData.cbSize = (UInt32)Marshal.SizeOf(msgData);
            msgData.hWnd = hwnd;
            msgData.rc = rc;
            msgData.uEdge = 0x3;
            SHAppBarMessage((UInt32)AppBarMessages.SetPos, ref msgData);
        }
    }
}

