using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RoundedTB
{
    public class SystemFns
    {
        public MainWindow mw;
        public string logPath;
        string m = "";

        public SystemFns()
        {
            mw = (MainWindow)Application.Current.MainWindow;
            logPath = Path.Combine(mw.localFolder, "rtb.log");
        }

        public Types.Settings ReadJSON()
        {
            string jsonSettings = File.ReadAllText(Path.Combine(mw.localFolder, "rtb.json"));
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
            File.Create(Path.Combine(mw.localFolder, "rtb.json")).Close();
            File.WriteAllText(Path.Combine(mw.localFolder, "rtb.json"), JsonConvert.SerializeObject(mw.activeSettings, Formatting.Indented));
        }

        public void FileSystem()
        {
            File.Create(logPath).Close();
            if (!File.Exists(Path.Combine(mw.localFolder, "rtb.json")))
            {
                if (mw.isWindows11)
                {
                    mw.activeSettings = new Types.Settings()
                    {
                        CornerRadius = 7,
                        MarginBasic = 3,
                        MarginBottom = 0,
                        MarginTop = 0,
                        MarginLeft = 0,
                        MarginRight = 0,
                        IsDynamic = false,
                        IsCentred = false,
                        ShowTray = false,
                        CompositionCompat = false,
                        IsNotFirstLaunch = false
                    };
                }
                else
                {
                    mw.activeSettings = new Types.Settings()
                    {
                        CornerRadius = 16,
                        MarginBasic = 2,
                        MarginBottom = 0,
                        MarginTop = 0,
                        MarginLeft = 0,
                        MarginRight = 0,
                        IsDynamic = false,
                        IsCentred = false,
                        ShowTray = false,
                        CompositionCompat = false,
                        IsNotFirstLaunch = false
                    };
                }
                
                WriteJSON(); // butts - Missy Quarry, 2020
            }
            if (File.ReadAllText(Path.Combine(mw.localFolder, "rtb.json")) == "" || File.ReadAllText(Path.Combine(mw.localFolder, "rtb.json")) == null)
            {
                WriteJSON(); // Initialises empty file
            }

        }

        public void addLog(string message)
        {
            m = $"[{DateTime.Now}] {message}\n";
            File.AppendAllText(logPath, m);
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

        public static IntPtr UpdateTranslucentTB(IntPtr taskbarHwnd)
        {
            return LocalPInvoke.SendMessage(LocalPInvoke.FindWindow("TTB_WorkerWindow", "TTB_WorkerWindow"), LocalPInvoke.RegisterWindowMessage("TTB_ForceRefreshTaskbar"), 0, taskbarHwnd);
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
                if (result == IntPtr.Zero)
                    throw new InvalidOperationException();

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
