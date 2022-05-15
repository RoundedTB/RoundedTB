using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RoundedTB
{
    public class Interaction
    {
        public MainWindow mw;
        string m = "";

        public Interaction()
        {
            try
            {
                mw = (MainWindow)Application.Current.MainWindow;
            }
            catch (Exception)
            {
                // No idea why this was necessary but it was so it's here now. Yay. TODO - work out why this is suddenly broken and unbreak it
            }
        }

        public Types.Settings ReadJSON()
        {
            string jsonSettings = File.ReadAllText(mw.configPath);
            Types.Settings settings = JsonConvert.DeserializeObject<Types.Settings>(jsonSettings);
            return settings;
        }

        public bool IsWindows11()
        {
            Debug.WriteLine(Environment.OSVersion.Version.Build);
            if (Environment.OSVersion.Version.Build >= 21996)
            {
                return true;
            }
            return false;
        }

        public void WriteJSON()
        {
            File.Create(mw.configPath).Close();
            File.WriteAllText(mw.configPath, JsonConvert.SerializeObject(mw.activeSettings, Formatting.Indented));
        }

        public void FileSystem()
        {
            File.Create(mw.logPath).Close();
            if (!File.Exists(mw.configPath))
            {
                if (mw.isWindows11)
                {
                    mw.activeSettings = new Types.Settings()
                    {
                        SimpleTaskbarLayout = new Types.SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        DynamicAppListLayout = new Types.SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        DynamicTrayLayout = new Types.SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        DynamicWidgetsLayout = new Types.SegmentSettings { CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
                        IsDynamic = false,
                        IsCentred = false,
                        IsWindows11 = true,
                        ShowTray = false,
                        CompositionCompat = false,
                        IsNotFirstLaunch = false,
                        FillOnMaximise = true,
                        FillOnTaskSwitch = true,
                        ShowSegmentsOnHover = false,
                        AutoHide = 0
                    };
                }
                else
                {
                    mw.activeSettings = new Types.Settings()
                    {
                        SimpleTaskbarLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        DynamicAppListLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        DynamicTrayLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        DynamicWidgetsLayout = new Types.SegmentSettings { CornerRadius = 16, MarginLeft = 2, MarginTop = 2, MarginRight = 2, MarginBottom = 2 },
                        IsDynamic = false,
                        IsCentred = false,
                        IsWindows11 = false,
                        ShowTray = false,
                        CompositionCompat = false,
                        IsNotFirstLaunch = false,
                        FillOnMaximise = true,
                        FillOnTaskSwitch = false,
                        ShowSegmentsOnHover = false,
                        AutoHide = 0
                    };
                }
                
                WriteJSON(); // butts - Missy Quarry, 2020
            }
            if (File.ReadAllText(mw.configPath) == "" || File.ReadAllText(mw.configPath) == null)
            {
                WriteJSON(); // Initialises empty file
            }

        }

        public static bool SetWorkspace(LocalPInvoke.RECT rect)
        {
            bool result = LocalPInvoke.SystemParametersInfo(LocalPInvoke.SPI_SETWORKAREA, 0, ref rect, LocalPInvoke.SPIF_change);
            if (!result)
            {
                // Get error
                Debug.WriteLine("Error setting work area: " + Marshal.GetLastWin32Error().ToString());
            }

            return result;
        }

        public void AddLog(string message)
        {
            //m = $"[{DateTime.Now}] {message}\n";
            //File.AppendAllText(mw.logPath, m);
        }

        public static bool IsTranslucentTBRunning()
        {
            Mutex mutex = null;
            try
            {
                return Mutex.TryOpenExisting("344635E9-9AE4-4E60-B128-D53E25AB70A7", out mutex);
            }
            finally
            {
                mutex?.Dispose();
            }
        }

        // Request that TranslucentTB forefully refesh the taskbar
        public static IntPtr UpdateTranslucentTB(IntPtr taskbarHwnd)
        {
            return LocalPInvoke.SendMessage(LocalPInvoke.FindWindow("TTB_WorkerWindow", "TTB_WorkerWindow"), LocalPInvoke.RegisterWindowMessage("TTB_ForceRefreshTaskbar"), 0, taskbarHwnd);
        }
        
        // Attempt to forcefully refresh the taskbar
        public static void UpdateLegacyTB(IntPtr taskbarHwnd)
        {
            const int WM_DWMCOMPOSITIONCHANGED = 789;
            LocalPInvoke.SendMessage(taskbarHwnd, WM_DWMCOMPOSITIONCHANGED, 1, IntPtr.Zero);
        }

        /// <summary>
        /// Calculates whether or not an integer is odd or even.
        /// </summary>
        /// <param name="input">
        /// The integer to be checked for oddness.
        /// </param>
        /// <returns>
        /// A nullable bool, which represents if the provided integer is odd. If the provided integer is neither even nor odd, then returns null.
        /// </returns>
        public bool? IsOdd(int input)
        {
            // The following section declares and initialises the required variables for the caculation.
            decimal comparison = input / 2; // A decimal, representing approximately half of the user's input.
            int check = Convert.ToInt32(comparison) * 2; // An integer-representation of the user's input value.

            // The following section tests for oddness by looking for differences in the prior-initialised values.
            if (check == input) // Checks if the "check" value is equal to the input.
            {
                return false; // Return false to indicate the value is not odd.
            }
            else if (check != input) // Repeat the above check in the event that quantum tunnelling has resulted in a variable changing.
            {
                return true; // Return true to indicate the value is odd.
            }
            return null; // Finally, return null to indicate that the provided number is neither odd nor even - not currently required, added for future-proofing in the event the concept of mathematics changes significantly enough to warrant it.
        // (this is a joke to annoy sylly)
        }

        public IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            switch (msg)
            {
                case WM_HOTKEY:
                    Debug.WriteLine(msg);
                    switch (wParam.ToInt32())
                    {
                        case 9000:
                            int vkey = ((int)lParam >> 16) & 0xFFFF;
                            Debug.WriteLine(vkey);
                            if (vkey == 0x71)
                            {
                                if (mw.showTrayCheckBox.IsChecked == true)
                                {
                                    mw.showTrayCheckBox.IsChecked = false;
                                }
                                else
                                {
                                    mw.showTrayCheckBox.IsChecked = true;
                                }
                                mw.ApplyButton_Click(null, null);
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public static bool IsAutoHideEnabled()
        {
            return Math.Abs(SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height) > 0;
        }

        public bool IsTaskbarVisibleOnMonitor(LocalPInvoke.RECT tbRectP, LocalPInvoke.RECT monitorRectP)
        {
            Rectangle tbRect = new Rectangle(tbRectP.Left + 3, tbRectP.Top + 3, tbRectP.Right - tbRectP.Left - 3, tbRectP.Bottom - tbRectP.Top - 3);
            Rectangle monitorRect = new Rectangle(monitorRectP.Left, monitorRectP.Top, monitorRectP.Right - monitorRectP.Left, monitorRectP.Bottom - monitorRectP.Top);
            return tbRect.IntersectsWith(monitorRect);
        }

        public delegate bool CallBack(int hwnd, int lParam);

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        public static List<IntPtr> GetTopLevelWindows()
        {
            List<IntPtr> AllActiveHandles = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(AllActiveHandles);
            try
            {
                EnumWindowsProc tlProc = new EnumWindowsProc(EnumWindow);
                LocalPInvoke.EnumWindows(tlProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                {
                    listHandle.Free();
                }
            }
            return AllActiveHandles;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            if (!(gch.Target is List<IntPtr> list))
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            return true;
        }

        public static bool TaskbarOnMonitorWithMaximisedWindow(IntPtr taskbarHwnd)
        {
            return true;
        }

        public enum TaskbarPosition
        {
            Unknown = -1,
            Left,
            Top,
            Right,
            Bottom,
        }

        public sealed class Taskbar
        {
            public Rectangle Bounds
            {
                get;
                private set;
            }
            public TaskbarPosition Position
            {
                get;
                private set;
            }
            public System.Drawing.Point Location
            {
                get
                {
                    return Bounds.Location;
                }
            }
            public System.Drawing.Size Size
            {
                get
                {
                    return Bounds.Size;
                }
            }

            //Always returns false under Windows 7
            public bool AlwaysOnTop
            {
                get;
                private set;
            }
            public bool AutoHide
            {
                get;
                private set;
            }

            public Taskbar(IntPtr taskbarHandle)
            {

                LocalPInvoke.APPBARDATA data = new LocalPInvoke.APPBARDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(LocalPInvoke.APPBARDATA));
                data.hWnd = taskbarHandle;
                IntPtr result = LocalPInvoke.SHAppBarMessage(LocalPInvoke.ABM.GetTaskbarPos, ref data);
                Position = (TaskbarPosition)data.uEdge;
                Bounds = Rectangle.FromLTRB(data.rc.Left, data.rc.Top, data.rc.Right, data.rc.Bottom);

                data.cbSize = (uint)Marshal.SizeOf(typeof(LocalPInvoke.APPBARDATA));
                result = LocalPInvoke.SHAppBarMessage(LocalPInvoke.ABM.GetState, ref data);
                int state = result.ToInt32();
                AlwaysOnTop = (state & LocalPInvoke.ABS.AlwaysOnTop) == LocalPInvoke.ABS.AlwaysOnTop;
                AutoHide = (state & LocalPInvoke.ABS.Autohide) == LocalPInvoke.ABS.Autohide;
            }
        }
    }
}
