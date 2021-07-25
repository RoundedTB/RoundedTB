using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RoundedTB
{
    public enum MONITOR_APP_VISIBILITY
    {
        MAV_UNKNOWN = 0,
        MAV_NO_APP_VISIBLE = 1,
        MAV_APP_VISIBLE = 2
    }

    [ComImport, Guid("6584CE6B-7D82-49C2-89C9-C6BC02BA8C38"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppVisibilityEvents
    {
        void AppVisibilityOnMonitorChanged(IntPtr hMonitor, MONITOR_APP_VISIBILITY previousMode, MONITOR_APP_VISIBILITY currentMode);
        void LauncherVisibilityChange([MarshalAs(UnmanagedType.Bool)] bool currentVisibleState);
    }

    [ComImport, Guid("2246EA2D-CAEA-4444-A3C4-6DE827E44313"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppVisibility
    {
        MONITOR_APP_VISIBILITY GetAppVisibilityOnMonitor(IntPtr hMonitor);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsLauncherVisible();

        uint Advise(IAppVisibilityEvents pCallback);
        void Unadvise(uint dwCookie);
    }

    [ComImport, Guid("7E5FE3D9-985F-4908-91F9-EE19F9FD1514"), ClassInterface(ClassInterfaceType.None)]
    public class AppVisibility { }
}
