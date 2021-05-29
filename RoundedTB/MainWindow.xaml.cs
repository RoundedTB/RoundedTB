using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using PInvoke;
using ModernWpf;
using System.Windows.Interop;


namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Taskbar> taskbarDetails = new List<Taskbar>();
        public bool shouldReallyDieNoReally = false;
        public string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public Settings activeSettings = new Settings();
        public BackgroundWorker bw = new BackgroundWorker();


        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Current.ActualApplicationThemeChanged += ThemeChanged();

            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk")))
            {
                StartupCheckBox.IsChecked = true;
                ShowMenuItem.Header = "Show RDP";
            }
            else
            {
                Visibility = Visibility.Visible;
            }
            bw.DoWork += Bw_DoWork;
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            FileSystem();
            activeSettings = ReadJSON();

            marginInput.Text = activeSettings.Margin.ToString();
            cornerRadiusInput.Text = activeSettings.CornerRadius.ToString();

            IntPtr hwndMain = FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            GetWindowRect(hwndMain, out RECT rectMain);
            GetWindowRgn(hwndMain, out IntPtr hrgnMain);
            
            taskbarDetails.Add(new Taskbar { TaskbarHwnd = hwndMain, TaskbarRect = rectMain, RecoveryHrgn = hrgnMain, ScaleFactor = Convert.ToDouble(GetDpiForWindow(hwndMain)) / 96.00, TaskbarRes = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}" });

            bool i = true;
            IntPtr hwndPrevious = IntPtr.Zero;
            while (i == true)
            {
                IntPtr hwndCurrent = FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_SecondaryTrayWnd", null);
                hwndPrevious = hwndCurrent;

                if (hwndCurrent == IntPtr.Zero)
                {
                    i = false;
                }
                else
                {
                    GetWindowRect(hwndCurrent, out RECT rectCurrent);
                    GetWindowRgn(hwndCurrent, out IntPtr hrgnCurrent);
                    taskbarDetails.Add(new Taskbar { TaskbarHwnd = hwndCurrent, TaskbarRect = rectCurrent, RecoveryHrgn = hrgnCurrent, ScaleFactor = Convert.ToDouble(GetDpiForWindow(hwndCurrent)) / 96.00, TaskbarRes = $"{rectCurrent.Right - rectCurrent.Left} x {rectCurrent.Bottom - rectCurrent.Top}" });
                }
            }
            if (marginInput.Text != null && cornerRadiusInput.Text != null)
            {
                ApplyButton_Click(null, null);
            }

        }

        private TypedEventHandler<ThemeManager, object> ThemeChanged()
        {
            if (ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
            {
                TrayIcon.Icon = DarkIcon.Icon;
            }
            else
            {
                TrayIcon.Icon = LightIcon.Icon;
            }
            return null;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(cornerRadiusInput.Text, out int roundFactor) || !int.TryParse(marginInput.Text, out int marginFactor))
            {
                return;
            }

            activeSettings.CornerRadius = roundFactor;
            activeSettings.Margin = marginFactor;

            foreach (var tbDeets in taskbarDetails)
            {
                UpdateTaskbar(tbDeets, marginFactor, roundFactor, tbDeets.TaskbarRect);
            }

            if (bw.IsBusy == false)
            {
                bw.RunWorkerAsync((marginFactor, roundFactor));
            }
            else
            {
                bw.CancelAsync();
                while (bw.IsBusy == true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
                bw.RunWorkerAsync((marginFactor, roundFactor));
            }

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (shouldReallyDieNoReally == false)
            {
                e.Cancel = true;
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show RTB";
            }
            WriteJSON();
        }

        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            while (true)
            {
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    for (int a = 0; a < taskbarDetails.Count; a++)
                    {
                        GetWindowRect(taskbarDetails[a].TaskbarHwnd, out RECT rectCheck);
                        if (rectCheck.Left != taskbarDetails[a].TaskbarRect.Left || rectCheck.Top != taskbarDetails[a].TaskbarRect.Top || rectCheck.Right != taskbarDetails[a].TaskbarRect.Right || rectCheck.Bottom != taskbarDetails[a].TaskbarRect.Bottom)
                        {
                            ResetTaskbar(taskbarDetails[a]);
                            taskbarDetails[a] = new Taskbar { TaskbarHwnd = taskbarDetails[a].TaskbarHwnd, TaskbarRect = rectCheck, RecoveryHrgn = taskbarDetails[a].RecoveryHrgn, ScaleFactor = GetDpiForWindow(taskbarDetails[a].TaskbarHwnd) / 96 };

                            UpdateTaskbar(taskbarDetails[a], (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item2, rectCheck);
                        }
                    }
                    System.Threading.Thread.Sleep(50);
                }

            }
        }

        public static void ResetTaskbar(Taskbar tbDeets)
        {
            SetWindowRgn(tbDeets.TaskbarHwnd, tbDeets.RecoveryHrgn, true);
        }

        public static void UpdateTaskbar(Taskbar tbDeets, int marginFactor, int roundFactor, RECT rectNew)
        {

            TaskbarEffectiveRegion ter = new TaskbarEffectiveRegion
            {
                EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                EffectiveTopLeft = Convert.ToInt32(marginFactor * tbDeets.ScaleFactor),
                EffectiveBottomRightX = Convert.ToInt32(rectNew.Right - rectNew.Left - (marginFactor * tbDeets.ScaleFactor)) + 1,
                EffectiveBottomRightY = Convert.ToInt32(rectNew.Bottom - rectNew.Top - (marginFactor * tbDeets.ScaleFactor)) + 1
            };


            SetWindowRgn(tbDeets.TaskbarHwnd, CreateRoundRectRgn(ter.EffectiveTopLeft, ter.EffectiveTopLeft , ter.EffectiveBottomRightX, ter.EffectiveBottomRightY, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius), true);
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            shouldReallyDieNoReally = true;
            foreach (var tbDeets in taskbarDetails)
            {
                ResetTaskbar(tbDeets);
            }
            Close();
        }

        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible == false)
            {
                Visibility = Visibility.Visible;
                ShowMenuItem.Header = "Hide RTB";
            }
            else
            {
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show RTB";
            }
        }

        private void Startup_Checked(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup)))
            {
                EnableStartup();
            }
        }

        private void Startup_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk"));
            }
            catch (Exception) { }
        }


        public Settings ReadJSON()
        {
            string jsonSettings = System.IO.File.ReadAllText(Path.Combine(localFolder, "rtb.json"));
            Settings settings = JsonConvert.DeserializeObject<Settings>(jsonSettings);
            return settings;
        }

        private void WriteJSON()
        {
            System.IO.File.Create(Path.Combine(localFolder, "rtb.json")).Close();
            System.IO.File.WriteAllText(Path.Combine(localFolder, "rtb.json"), JsonConvert.SerializeObject(activeSettings, Formatting.Indented));
        }

        private void FileSystem()
        {

            if (!System.IO.File.Exists(Path.Combine(localFolder, "rtb.json")))
            {
                WriteJSON(); // butts - Missy Quarry, 2020
            }
            if (System.IO.File.ReadAllText(Path.Combine(localFolder, "rtb.json")) == "" || System.IO.File.ReadAllText(Path.Combine(localFolder, "rtb.json")) == null)
            {
                WriteJSON(); // Initialises empty file
            }

        }

        public void EnableStartup()
        {
            try
            {
                string shortcutFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!Directory.Exists(shortcutFolder))
                {
                    Directory.CreateDirectory(shortcutFolder);
                }
                WshShell shellClass = new WshShell();
                string rtbStartupLink = Path.Combine(shortcutFolder, "RoundedTB.lnk");
                IWshShortcut shortcut = (IWshShortcut)shellClass.CreateShortcut(rtbStartupLink);
                shortcut.TargetPath = Environment.GetCommandLineArgs()[0];
                shortcut.IconLocation = Environment.GetCommandLineArgs()[0];
                shortcut.Arguments = "";
                shortcut.Description = "Start RoundedTB";
                shortcut.Save();
            }
            catch (Exception)
            {
            }
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x001A) // WM_SETTINGCHANGE
            {
                
            }

            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        static extern int GetWindowRgn(IntPtr hWnd, out IntPtr hRgn);

        [DllImport("user32.dll")]
        static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowExA(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, [In] ref RECT lprcUpdate, IntPtr hrgnUpdate, Rdw flags);

        [Flags()]
        private enum Rdw : uint
        {
            /// <summary>
            /// Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
            /// </summary>
            Invalidate = 0x1,

            /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
            InternalPaint = 0x2,

            /// <summary>
            /// Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
            /// Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
            /// </summary>
            Erase = 0x4,

            /// <summary>
            /// Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
            /// This value does not affect internal WM_PAINT messages.
            /// </summary>
            Validate = 0x8,

            NoInternalPaint = 0x10,

            /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
            NoErase = 0x20,

            /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
            NoChildren = 0x40,

            /// <summary>Includes child windows, if any, in the repainting operation.</summary>
            AllChildren = 0x80,

            /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
            UpdateNow = 0x100,

            /// <summary>
            /// Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
            /// The affected windows receive WM_PAINT messages at the ordinary time.
            /// </summary>
            EraseNow = 0x200,

            Frame = 0x400,

            NoFrame = 0x800
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public class Taskbar
        {
            public IntPtr TaskbarHwnd { get; set; }
            public RECT TaskbarRect { get; set; }
            public IntPtr RecoveryHrgn { get; set; }
            public double ScaleFactor { get; set; }
            public string TaskbarRes { get; set; }
        }

        public class Settings
        {
            public int CornerRadius { get; set; }
            public int Margin { get; set; }
        }

        public class TaskbarEffectiveRegion
        {
            public int EffectiveCornerRadius { get; set; }
            public int EffectiveTopLeft { get; set; }
            public int EffectiveBottomRightX { get; set; }
            public int EffectiveBottomRightY { get; set; }
        }

        private void DebugMenuItem_Click(object sender, RoutedEventArgs e)
        {

            
            IntPtr hwndNext = FindWindowExA(taskbarDetails[0].TaskbarHwnd, IntPtr.Zero, "Start", null);
            List<IntPtr> bitsOfTaskbar = new List<IntPtr>();
            bitsOfTaskbar.Add(hwndNext);
            while (true) 
            {
                hwndNext = FindWindowExA(taskbarDetails[0].TaskbarHwnd, hwndNext, null, null);
                if (bitsOfTaskbar.Contains(hwndNext))
                {
                    break;
                }
                bitsOfTaskbar.Add(hwndNext);

            }
            foreach (IntPtr hwnd in bitsOfTaskbar)
            {
                GetWindowRect(hwnd, out RECT rect);
                MoveWindow(hwnd, rect.Left + 50, rect.Top, (rect.Right + 50) - (rect.Left + 50), rect.Bottom - rect.Top, true);
            }
        }
    }
}
