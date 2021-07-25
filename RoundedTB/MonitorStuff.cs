using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace RoundedTB
{
    class MonitorStuff
    {
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lplmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public LocalPInvoke.RECT rcMonitor;
            public LocalPInvoke.RECT rcWork;
            public uint dwFlags;
        }

        // Stuff for acquiring mouse position because Cursor.Position failed me
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        // It's like a normal bool but delegate, perhaps its also delicate? I don't know. That's up to you, I suppose!
        public delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref LocalPInvoke.RECT lprcMonitor, IntPtr dwData);

        // Gets a list of display info
        public static DisplayInfoCollection GetDisplays()
        {
            DisplayInfoCollection col = new DisplayInfoCollection();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref LocalPInvoke.RECT lprcMonitor, IntPtr dwData)
                {
                    MONITORINFO mi = new MONITORINFO();
                    mi.cbSize = (uint)Marshal.SizeOf(mi);
                    bool success = GetMonitorInfo(hMonitor, ref mi);
                    if (success)
                    {
                        DisplayInfo di = new DisplayInfo
                        {
                            ScreenWidth = (mi.rcMonitor.Right - mi.rcMonitor.Left).ToString(),
                            ScreenHeight = (mi.rcMonitor.Bottom - mi.rcMonitor.Top).ToString(),
                            MonitorArea = mi.rcMonitor,
                            WorkArea = mi.rcWork,
                            Availability = mi.dwFlags.ToString(),
                            Handle = hMonitor,
                            Top = mi.rcMonitor.Top,
                            Left = mi.rcMonitor.Left
                        };
                        col.Add(di);
                    }
                    return true;
                }, IntPtr.Zero);
            return col;
        }

        // Super-handy to do things or something
        public class DisplayInfoCollection : List<DisplayInfo>
        {
        }

        // What the above is made of
        public class DisplayInfo
        {
            public string Availability { get; set; }
            public string ScreenHeight { get; set; }
            public string ScreenWidth { get; set; }
            public LocalPInvoke.RECT MonitorArea { get; set; }
            public LocalPInvoke.RECT WorkArea { get; set; }
            public IntPtr Handle { get; set; }
            public int Top { get; set; }
            public int Left { get; set; }
        }
    }
}
