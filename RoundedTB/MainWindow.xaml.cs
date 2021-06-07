using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using PInvoke;
using System.Reflection;
using ModernWpf;
using System.Windows.Interop;
using DesktopBridge;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using System.Windows.Input;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// If the Start Menu was visible before re-checking
        /// </summary>
        private static bool? wasStartMenuVisibleBefore = null;

        public List<Taskbar> taskbarDetails = new List<Taskbar>();
        public bool shouldReallyDieNoReally = false;
        public string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public Settings activeSettings = new Settings();
        public BackgroundWorker bw = new BackgroundWorker();
        public IntPtr hwndDesktopButton = IntPtr.Zero;
        public IntPtr hwndMain = IntPtr.Zero;
        public IntPtr hwndMainSecond = IntPtr.Zero;
        int numberToForceRefresh = 0;

        public MainWindow()
        {
            InitializeComponent();
            TrayIconCheck();
            if (IsRunningAsUWP())
            {
                StartupInit(true);
                localFolder = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
            }
            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk")) && !IsRunningAsUWP())
            {
                StartupCheckBox.IsChecked = true;
                ShowMenuItem.Header = "Show RTB";
            }
            else
            {
                if (!IsRunningAsUWP())
                {
                    Visibility = Visibility.Visible;

                }
            }
            bw.DoWork += Bw_DoWork;
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            FileSystem();
            activeSettings = ReadJSON();

            marginInput.Text = activeSettings.Margin.ToString();
            cornerRadiusInput.Text = activeSettings.CornerRadius.ToString();
            completeHideCheckBox.IsChecked = activeSettings.CompleteHide;
            GenerateTaskbarInfo();
            if (marginInput.Text != null && cornerRadiusInput.Text != null)
            {
                ApplyButton_Click(null, null);
            }
        }

        private TypedEventHandler<ThemeManager, object> TrayIconCheck()
        {
            Uri resLight = new Uri("pack://application:,,,/res/traylight.ico");
            Uri resDark = new Uri("pack://application:,,,/res/traydark.ico");

            if (ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light)
            {
                TrayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(resLight).Stream);
            }
            else
            {
                TrayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(resDark).Stream);
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
            activeSettings.CompleteHide = (bool)completeHideCheckBox.IsChecked;

            if (!(bool)completeHideCheckBox.IsChecked)
                for (int a = 0; a < taskbarDetails.Count; a++)
                    ShowWindow(taskbarDetails[a].TaskbarHwnd, 5);

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
            else
            {
                User32.SetWindowPos(hwndDesktopButton, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_SHOWWINDOW);
                for (int a = 0; a < taskbarDetails.Count; a++)
                    ShowWindow(taskbarDetails[a].TaskbarHwnd, 5);
            }
            WriteJSON();
        }

        // Handles keeping the taskbar updated in the background
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
                    MonitorStuff.DisplayInfoCollection Displays = MonitorStuff.GetDisplays();
                    for (int a = 0; a < taskbarDetails.Count; a++)
                    {
                        if (!IsWindow(taskbarDetails[a].TaskbarHwnd) || AreThereNewTaskbars(taskbarDetails[a].TaskbarHwnd))
                        {
                            Displays = MonitorStuff.GetDisplays();
                            System.Threading.Thread.Sleep(5000);
                            GenerateTaskbarInfo();
                            numberToForceRefresh = taskbarDetails.Count + 1;
                            goto LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH; // consider this a double-break, it's literally just a few lines below STOP COMPLAINING
                        }

                        IntPtr currentMonitor = MonitorFromWindow(taskbarDetails[a].TaskbarHwnd, 0x2);
                        GetWindowRect(taskbarDetails[a].TaskbarHwnd, out RECT rectCheck);
                        foreach (MonitorStuff.DisplayInfo Display in Displays) // This loop checks for if the taskbar is "hidden" offscreen
                        {
                            if (Display.Handle == currentMonitor)
                            {
                                POINT pt = new POINT { x = rectCheck.Left + ((rectCheck.Right - rectCheck.Left) / 2), y = rectCheck.Top + ((rectCheck.Bottom - rectCheck.Top) / 2) };
                                RECT refRect = Display.MonitorArea;
                                bool isOnTaskbar = PtInRect(ref refRect, pt);
                                if (!isOnTaskbar)
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        if (activeSettings.CompleteHide)
                                            ShowWindow(taskbarDetails[a].TaskbarHwnd, 0);
                                    });
                                    
                                    ResetTaskbar(taskbarDetails[a]);
                                    goto LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH; // consider this a double-break, it's literally just a few lines below STOP COMPLAINING
                                }
                            }
                        }

                        // If the taskbar moves, reset it the n
                        if (rectCheck.Left != taskbarDetails[a].TaskbarRect.Left || rectCheck.Top != taskbarDetails[a].TaskbarRect.Top || rectCheck.Right != taskbarDetails[a].TaskbarRect.Right || rectCheck.Bottom != taskbarDetails[a].TaskbarRect.Bottom || numberToForceRefresh > 0)
                        {
                            ResetTaskbar(taskbarDetails[a]);
                            taskbarDetails[a] = new Taskbar { TaskbarHwnd = taskbarDetails[a].TaskbarHwnd, TaskbarRect = rectCheck, RecoveryHrgn = taskbarDetails[a].RecoveryHrgn, ScaleFactor = GetDpiForWindow(taskbarDetails[a].TaskbarHwnd) / 96 };
                            UpdateTaskbar(taskbarDetails[a], (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item2, rectCheck);
                            numberToForceRefresh--;
                            this.Dispatcher.Invoke(() =>
                            {
                                if (activeSettings.CompleteHide)
                                    ShowWindow(taskbarDetails[a].TaskbarHwnd, 5);
                            });
                        }

                    LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH:
                        { };
                    }
                    System.Threading.Thread.Sleep(100);
                }

            }
        }

        public bool AreThereNewTaskbars(IntPtr checkAfterTaskbar)
        {
            List<IntPtr> currentTaskbars = new List<IntPtr>();
            bool i = true;
            IntPtr hwndPrevious = IntPtr.Zero;
            currentTaskbars.Add(FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_TrayWnd", null));

            while (i)
            {
                IntPtr hwndCurrent = FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_SecondaryTrayWnd", null);
                hwndPrevious = hwndCurrent;

                if (hwndCurrent == IntPtr.Zero)
                {
                    i = false;
                }
                else
                {
                    currentTaskbars.Add(hwndCurrent);
                }
            }

            if (currentTaskbars.Count > taskbarDetails.Count)
            {
                return true;
            }
            return false;
        }

        public void GenerateTaskbarInfo()
        {
            taskbarDetails.Clear();
            IntPtr hwndMain = FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            hwndDesktopButton = FindWindowExA(FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null), IntPtr.Zero, "TrayShowDesktopButtonWClass", null);
            User32.SetWindowPos(hwndDesktopButton, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_HIDEWINDOW); // Hide "Show Desktop" button
            GetWindowRect(hwndMain, out RECT rectMain);
            GetWindowRgn(hwndMain, out IntPtr hrgnMain);

            taskbarDetails.Add(new Taskbar { TaskbarHwnd = hwndMain, TaskbarRect = rectMain, RecoveryHrgn = hrgnMain, ScaleFactor = Convert.ToDouble(GetDpiForWindow(hwndMain)) / 96.00, TaskbarRes = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}" });

            bool i = true;
            IntPtr hwndPrevious = IntPtr.Zero;
            while (i)
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


            SetWindowRgn(tbDeets.TaskbarHwnd, CreateRoundRectRgn(ter.EffectiveTopLeft, ter.EffectiveTopLeft, ter.EffectiveBottomRightX, ter.EffectiveBottomRightY, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius), true);
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

        private async void Startup_Checked(object sender, RoutedEventArgs e)
        {
            if (IsRunningAsUWP())
            {
                await StartupToggle();
                await StartupInit(false);
            }
            else
            {
                if (!System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Startup)))
                {
                    EnableStartup();
                }
                else
                {
                    try
                    {
                        System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk"));
                    }
                    catch (Exception) { }
                }
            }

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

        async Task StartupToggle()
        {
            StartupTask startupTask = await StartupTask.GetAsync("RTB"); // Pass the task ID you specified in the appxmanifest file
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    StartupTaskState newState = await startupTask.RequestEnableAsync();
                    StartupCheckBox.IsEnabled = true;
                    break;

                case StartupTaskState.DisabledByUser:
                    StartupCheckBox.IsEnabled = false;
                    break;

                case StartupTaskState.EnabledByPolicy:
                    StartupCheckBox.IsEnabled = false;
                    break;

                case StartupTaskState.DisabledByPolicy:
                    StartupCheckBox.IsEnabled = false;
                    break;

                case StartupTaskState.Enabled:
                    startupTask.Disable();
                    StartupCheckBox.IsEnabled = true;
                    break;
            }
        }

        async Task StartupInit(bool clean)
        {
            StartupTask startupTask = await StartupTask.GetAsync("RTB");
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = true;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        ShowMenuItem.Header = "Hide RTB";
                    }
                    StartupCheckBox.Content = "Run on startup";
                    break;

                case StartupTaskState.DisabledByUser:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        ShowMenuItem.Header = "Hide RTB";
                    }
                    StartupCheckBox.Content = "Startup unavailable";
                    break;

                case StartupTaskState.EnabledByPolicy:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Hidden;
                        ShowMenuItem.Header = "Show RTB";
                    }
                    StartupCheckBox.Content = "Startup mandatory";
                    break;

                case StartupTaskState.DisabledByPolicy:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        ShowMenuItem.Header = "Hide RTB";
                    }
                    StartupCheckBox.Content = "Startup unavailable";
                    break;

                case StartupTaskState.Enabled:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = true;
                    if (clean)
                    {
                        Visibility = Visibility.Hidden;
                        ShowMenuItem.Header = "Show RTB";
                    }
                    StartupCheckBox.Content = "Run on startup";
                    break;
            }
        }

        // Checks if running as a UWP app
        public bool IsRunningAsUWP()
        {
            try
            {
                Helpers helpers = new Helpers();
                return helpers.IsRunningAsUwp();
            }
            catch (Exception)
            {
                return false;
            }

        }

        [DllImport("user32.dll")]
        static extern bool PtInRect(ref RECT lprc, POINT pt);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);

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
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

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
            public bool CompleteHide { get; set; }
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

        private async void ContextMenu_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsRunningAsUWP())
            {
                await StartupInit(false);
            }
        }
    }
}
