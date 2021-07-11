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
using System.Windows.Threading;
using System.Windows.Interop;
using DesktopBridge;
using System.Threading.Tasks;
using Windows.ApplicationModel;


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
        public IntPtr hwndDesktopButton = IntPtr.Zero;
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

            if (marginInput.Text.ToLower() != "advanced")
            {
                marginInput.Text = activeSettings.MarginTop.ToString();
                marginInput.Text = activeSettings.MarginLeft.ToString();
                marginInput.Text = activeSettings.MarginBottom.ToString();
                marginInput.Text = activeSettings.MarginRight.ToString();
            }
            else
            {
                mTopInput.Text = activeSettings.MarginTop.ToString();
                mLeftInput.Text = activeSettings.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.MarginBottom.ToString();
                mRightInput.Text = activeSettings.MarginRight.ToString();
            }
            

            cornerRadiusInput.Text = activeSettings.CornerRadius.ToString();
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
            int mt = 0;
            int ml = 0;
            int mb = 0;
            int mr = 0;

            if (!int.TryParse(cornerRadiusInput.Text, out int roundFactor) || (!int.TryParse(marginInput.Text, out int marginFactor) && marginInput.Text.ToLower() != "advanced"))
            {
                return;
            }

            activeSettings.CornerRadius = roundFactor;
            if (marginInput.Text.ToLower() != "advanced")
            {
                mt = marginFactor;
                ml = marginFactor;
                mb = marginFactor;
                mr = marginFactor;
            }
            else
            {
                if (!int.TryParse(mTopInput.Text, out mt) || !int.TryParse(mLeftInput.Text, out ml) || !int.TryParse(mBottomInput.Text, out mb) || !int.TryParse(mRightInput.Text, out mr))
                {
                    return;
                }
            }

            foreach (var tbDeets in taskbarDetails)
            {
                UpdateTaskbar(tbDeets, mt, ml, mb, mr, roundFactor, tbDeets.TaskbarRect);
            }

            if (bw.IsBusy == false && marginInput.Text.ToLower() != "advanced")
            {
                bw.RunWorkerAsync((marginFactor, roundFactor));
            }
            else if (marginInput.Text.ToLower() != "advanced")
            {
                bw.CancelAsync();
                while (bw.IsBusy == true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
                bw.RunWorkerAsync((marginFactor, roundFactor));
            }
            else
            {
                bw.CancelAsync();
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
                                    ResetTaskbar(taskbarDetails[a]);
                                    goto LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH; // consider this a double-break, it's literally just a few lines below STOP COMPLAINING
                                }
                            }
                        }
                        // If the taskbar moves, reset it then restore it
                        if (marginInput.Text.ToLower() != "advanced" && (rectCheck.Left != taskbarDetails[a].TaskbarRect.Left || rectCheck.Top != taskbarDetails[a].TaskbarRect.Top || rectCheck.Right != taskbarDetails[a].TaskbarRect.Right || rectCheck.Bottom != taskbarDetails[a].TaskbarRect.Bottom || numberToForceRefresh > 0))
                        {
                            ResetTaskbar(taskbarDetails[a]);
                            taskbarDetails[a] = new Taskbar { TaskbarHwnd = taskbarDetails[a].TaskbarHwnd, TaskbarRect = rectCheck, RecoveryHrgn = taskbarDetails[a].RecoveryHrgn, ScaleFactor = GetDpiForWindow(taskbarDetails[a].TaskbarHwnd) / 96 };

                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => UpdateTaskbar(taskbarDetails[a], (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item2, rectCheck)));

                            
                            numberToForceRefresh--;
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
            foreach (Taskbar tb in taskbarDetails)
            {
                try
                {
                    //tb.TaskbarEffectWindow.Close();
                }
                catch (Exception) { }
            }
            taskbarDetails.Clear();
            IntPtr hwndMain = FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            hwndDesktopButton = FindWindowExA(FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null), IntPtr.Zero, "TrayShowDesktopButtonWClass", null);
            User32.SetWindowPos(hwndDesktopButton, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_HIDEWINDOW); // Hide "Show Desktop" button
            GetWindowRect(hwndMain, out RECT rectMain);
            GetWindowRgn(hwndMain, out IntPtr hrgnMain);

            taskbarDetails.Add(new Taskbar { TaskbarHwnd = hwndMain, TaskbarRect = rectMain, RecoveryHrgn = hrgnMain, ScaleFactor = Convert.ToDouble(GetDpiForWindow(hwndMain)) / 96.00, TaskbarRes = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}", TaskbarEffectWindow = new TaskbarEffect()});

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
                    taskbarDetails.Add(new Taskbar { TaskbarHwnd = hwndCurrent, TaskbarRect = rectCurrent, RecoveryHrgn = hrgnCurrent, ScaleFactor = Convert.ToDouble(GetDpiForWindow(hwndCurrent)) / 96.00, TaskbarRes = $"{rectCurrent.Right - rectCurrent.Left} x {rectCurrent.Bottom - rectCurrent.Top}", TaskbarEffectWindow = new TaskbarEffect() });
                }
            }

        }

        public static void ResetTaskbar(Taskbar tbDeets)
        {
            SetWindowRgn(tbDeets.TaskbarHwnd, tbDeets.RecoveryHrgn, true);
        }

        public static void UpdateTaskbar(Taskbar tbDeets, int mTopFactor, int mLeftFactor, int mBottomFactor, int mRightFactor, int roundFactor, RECT rectNew)
        {

            TaskbarEffectiveRegion ter = new TaskbarEffectiveRegion
            {
                EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                EffectiveTop = Convert.ToInt32(mTopFactor * tbDeets.ScaleFactor),
                EffectiveLeft = Convert.ToInt32(mLeftFactor * tbDeets.ScaleFactor),
                EffectiveRight = Convert.ToInt32(rectNew.Right - rectNew.Left - (mRightFactor * tbDeets.ScaleFactor)) + 1,
                EffectiveBottom = Convert.ToInt32(rectNew.Bottom - rectNew.Top - (mBottomFactor * tbDeets.ScaleFactor)) + 1
            };
            IntPtr effectHandle = new WindowInteropHelper(tbDeets.TaskbarEffectWindow).Handle;

            SetWindowRgn(tbDeets.TaskbarHwnd, CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop , ter.EffectiveRight, ter.EffectiveBottom, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius), true);
            SetWindowRgn(effectHandle, CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveRight, ter.EffectiveBottom, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius), true);
            MoveWindow(effectHandle, rectNew.Left, rectNew.Top, rectNew.Right - rectNew.Left, rectNew.Bottom - rectNew.Top, true);
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
            public TaskbarEffect TaskbarEffectWindow { get; set; }
    }

        public class Settings
        {
            public int CornerRadius { get; set; }
            public int MarginBottom { get; set; }
            public int MarginLeft { get; set; }
            public int MarginRight { get; set; }
            public int MarginTop { get; set; }
        }

        public class TaskbarEffectiveRegion
        {
            public int EffectiveCornerRadius { get; set; }
            public int EffectiveTop { get; set; }
            public int EffectiveLeft { get; set; }
            public int EffectiveRight { get; set; }
            public int EffectiveBottom { get; set; }
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

        private void advancedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Width < 300)
            {
                marginInput.Text = "Advanced";
                marginInput.IsEnabled = false;
                Width = 393;
                AdvancedGrid.Visibility = Visibility.Visible;
            }
            else
            {
                marginInput.Text = "0";
                marginInput.IsEnabled = true;
                Width = 169;
                AdvancedGrid.Visibility = Visibility.Collapsed;
            }
        }
    }
}
