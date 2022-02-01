using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Reflection;
using ModernWpf;
using System.Windows.Threading;
using System.Windows.Interop;
using DesktopBridge;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using WPFUI;
using System.Windows.Media;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Many thanks to
    ///  - FloatingMilkshake
    ///  - cardin
    ///  - cleverActon0126
    ///  for your gracious donations! 💖
    ///  
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isWindows11;
        public List<Types.Taskbar> taskbarDetails = new List<Types.Taskbar>();
        public bool shouldReallyDieNoReally = false;
        public string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.json");
        public string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.log");
        public Types.Settings activeSettings = new Types.Settings();
        public BackgroundWorker taskbarThread = new BackgroundWorker();
        public IntPtr hwndDesktopButton = IntPtr.Zero;
        public int lastDynDistance = 0;
        public int numberToForceRefresh = 0;
        public bool isCentred = false;
        public bool isAlreadyRunning = false;
        public Background background;
        public Interaction interaction;
        private HwndSource source;
        public int version = -1;
        /// <summary>
        /// Versions:
        /// -1: Canary
        ///  0: R3.0
        ///  1: P3.1B
        ///  2: R3.1
        /// </summary>

        public MainWindow()
        {
            WPFUI.Background.Manager.Apply(WPFUI.Background.BackgroundType.Mica, this);

            InitializeComponent();


            // Check OS build, as behaviours rather-annoyingly differ between Windows 11 and Windows 10
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var buildNumber = registryKey.GetValue("CurrentBuild").ToString();
            if (Convert.ToInt32(buildNumber) >= 21996)
            {
                isWindows11 = true;
            }
            else
            {
                isWindows11 = false;
                activeSettings.IsWindows11 = false;
                dynamicCheckBox.Content = "Split mode";
                fillAltTabCheckBox.Content = "[Unavailable]";
            }

            // Initialise functions
            background = new Background();
            interaction = new Interaction();

            // Check if RoundedTB is already running, and if it is, do nothing.
            Process[] matchingProcesses = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            
            if (matchingProcesses.Length > 1)
            {
                List<IntPtr> windowList = Interaction.GetTopLevelWindows();
                foreach (IntPtr hwnd in windowList)
                {
                    StringBuilder windowClass = new StringBuilder(1024);
                    StringBuilder windowTitle = new StringBuilder(1024);
                    try
                    {
                        LocalPInvoke.GetClassName(hwnd, windowClass, 1024);
                        LocalPInvoke.GetWindowText(hwnd, windowTitle, 1024);

                        if (windowClass.ToString().Contains("HwndWrapper[RoundedTB.exe") && windowTitle.ToString() == "RoundedTB")
                        {
                            LocalPInvoke.SetWindowText(hwnd, "RoundedTB_SettingsRequest");
                        }
                    }
                    catch (Exception) { }
                }
                shouldReallyDieNoReally = true;
                isAlreadyRunning = true;
                Close();
                return;
            }
            TrayIconCheck();

            if (IsRunningAsUWP())
            {
                #pragma warning disable CS4014
                StartupInit(true);
                configPath = Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "rtb.json");
                logPath = Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "rtb.log");
            }

            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk")) && !IsRunningAsUWP())
            {
                StartupCheckBox.IsChecked = true;
                ShowMenuItem.Header = "Show RoundedTB";
            }
            taskbarThread.WorkerSupportsCancellation = true;
            taskbarThread.WorkerReportsProgress = true;
            taskbarThread.DoWork +=background.DoWork;

            // Load settings into memory/UI
            interaction.FileSystem();
            if (!IsRunningAsUWP())
            {
                interaction.AddLog($"RoundedTB started!");
            }
            else
            {
                interaction.AddLog($"RoundedTB started in UWP mode!");
            }
            activeSettings = interaction.ReadJSON();

            if (isWindows11)
            {
                activeSettings.IsWindows11 = true;
            }
            else
            {
                activeSettings.IsWindows11 = false;
            }
            // Default settings
            if (activeSettings == null)
            {
                
                if (isWindows11) // Default settings for Windows 11
                {
                    activeSettings = new Types.Settings()
                    {
                        SimpleTaskbarLayout = new Types.SegmentSettings{ CornerRadius = 7, MarginLeft = 3, MarginTop = 3, MarginRight = 3, MarginBottom = 3 },
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
                        ShowTrayOnHover = false
                    };
                }
                else // Default settings for Windows 10
                {
                    activeSettings = new Types.Settings()
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
                        ShowTrayOnHover = false
                    };
                }
            }

            if (version != activeSettings.Version && version != -1)
            {
                activeSettings.IsNotFirstLaunch = false;
            }
            activeSettings.Version = version;


            interaction.AddLog($"Settings loaded:");
            interaction.AddLog(
                $"SimpleTaskbarLayout: {activeSettings.SimpleTaskbarLayout}\n" +
                $"DynamicAppListLayout: {activeSettings.DynamicAppListLayout}\n" +
                $"DynamicTrayLayout: {activeSettings.DynamicTrayLayout}\n" +
                $"DynamicWidgetsLayout: {activeSettings.DynamicWidgetsLayout}\n" +
                $"IsDynamic: {activeSettings.IsDynamic}\n" +
                $"IsCentred: {activeSettings.IsCentred}\n" +
                $"ShowTray: {activeSettings.ShowTray}\n" +
                $"CompositionCompat: {activeSettings.CompositionCompat}\n" +
                $"IsNotFirstLaunch: {activeSettings.IsNotFirstLaunch}\n" +
                $"FillOnMaximise: {activeSettings.FillOnMaximise}\n" +
                $"FillOnTaskSwitch: {activeSettings.FillOnTaskSwitch}\n" +
                $"ShowTrayOnHover: {activeSettings.ShowTrayOnHover}\n"
                );

            // Checks if advanced margins are configured
            if (true)
            {
                mTopInput.IsEnabled = true;
                mLeftInput.IsEnabled = true;
                mBottomInput.IsEnabled = true;
                mRightInput.IsEnabled = true;

                // TODO: Reimplement this
                //mTopInput.Text = activeSettings.MarginTop.ToString();
                //mLeftInput.Text = activeSettings.MarginLeft.ToString();
                //mBottomInput.Text = activeSettings.MarginBottom.ToString();
                //mRightInput.Text = activeSettings.MarginRight.ToString();

            }

            // Get whether or not taskbar is centred
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced"))
                {
                    if (key != null)
                    {
                        int val = (int)key.GetValue("TaskbarAl");
                        if (val == 1)
                        {
                            isCentred = true;
                        }
                        else
                        {
                            isCentred = false;
                        }
                        interaction.AddLog($"Taskbar centred? {isCentred}");
                    }
                }
            }
            catch (Exception aaaa)
            {
                interaction.AddLog(aaaa.Message);
            }
            if (!isWindows11)
            {
                activeSettings.IsCentred = false;
            }

            // Copy and apply settings to UI
            dynamicCheckBox.IsChecked = activeSettings.IsDynamic;
            centredCheckBox.IsChecked = activeSettings.IsCentred;
            showTrayCheckBox.IsChecked = activeSettings.ShowTray;
            fillMaximisedCheckBox.IsChecked = activeSettings.FillOnMaximise;
            fillAltTabCheckBox.IsChecked = activeSettings.FillOnTaskSwitch;
            showTrayOnHoverCheckBox.IsChecked = activeSettings.ShowTrayOnHover;
            compositionFixCheckBox.IsChecked = activeSettings.CompositionCompat;
            taskbarDetails = Taskbar.GenerateTaskbarInfo();
            if (true)
            {
                ApplyButton_Click(null, null);
            }

            if (!activeSettings.FillOnMaximise)
            {
                activeSettings.FillOnTaskSwitch = false;
                fillAltTabCheckBox.IsEnabled = false;
            }

            //Showhide the split mode help button
            if (!isWindows11 && activeSettings.IsDynamic)
            {
                splitHelpButton.Visibility = Visibility.Visible;
            }
            else
            {
                splitHelpButton.Visibility = Visibility.Hidden;
            }

            if (activeSettings.IsNotFirstLaunch != true)
            {
                activeSettings.IsNotFirstLaunch = true;
                AboutWindow aw = new AboutWindow();
                aw.expander0.IsExpanded = true;
                aw.ShowDialog();
                try
                {
                    Visibility = Visibility.Visible;
                }
                catch (InvalidOperationException)
                {

                }
                ShowMenuItem.Header = "Hide RoundedTB";
            }

            if (activeSettings.AutoHide > 0)
            {
                AutoHide(true, taskbarDetails);
            }
            UpdateUi();

        }

        public void UpdateUi()
        {
            if (activeSettings.ShowTrayOnHover)
            {
                trayRectStandIn.Visibility = Visibility.Visible;
                trayRectStandIn.Opacity = 0.25;
            }
            else if (activeSettings.ShowTray)
            {
                trayRectStandIn.Visibility = Visibility.Visible;
                trayRectStandIn.Opacity = 1;
            }
            else
            {
                trayRectStandIn.Visibility = Visibility.Hidden;
                trayRectStandIn.Opacity = 1;
            }

            if (activeSettings.IsCentred && activeSettings.IsWindows11 && activeSettings.IsDynamic)
            {
                taskbarRectStandIn.Margin = new Thickness(126, 0, 126, 5);
                trayRectStandIn.Visibility = Visibility.Visible;
                widgetsRectStandIn.Visibility = Visibility.Visible;
            }
            else if (activeSettings.IsDynamic)
            {
                taskbarRectStandIn.Margin = new Thickness(5, 0, 247, 5);
                trayRectStandIn.Visibility = Visibility.Visible;
                widgetsRectStandIn.Visibility = Visibility.Visible;
            }
            else
            {
                taskbarRectStandIn.Margin = new Thickness(5, 210, 5, 5);
                trayRectStandIn.Visibility = Visibility.Hidden;
                widgetsRectStandIn.Visibility = Visibility.Hidden;

            }
        }

        public void AutoHide(bool enable, List<Types.Taskbar> taskbarDetails)
        {
            // Wondering how this works, are you? Perhaps come up with your own ideas instead of copying other people, you uninspired hack 😒
            if (enable)
            {
                MonitorStuff.DisplayInfoCollection Displays = MonitorStuff.GetDisplays();

                foreach (MonitorStuff.DisplayInfo display in Displays)
                {
                    LocalPInvoke.RECT workArea = display.MonitorArea;
                    workArea.Bottom = workArea.Bottom - 2;
                    Interaction.SetWorkspace(workArea);
                }
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    LocalPInvoke.SetWindowPos(taskbar.TaskbarHwnd, new IntPtr(-1), 0, 0, 0, 0, LocalPInvoke.SetWindowPosFlags.IgnoreMove | LocalPInvoke.SetWindowPosFlags.IgnoreResize);
                    Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AlwaysOnTop, taskbar.TaskbarHwnd);
                }
            }
            else
            {
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    LocalPInvoke.SetWindowPos(taskbar.TaskbarHwnd, new IntPtr(-1), 0, 0, 0, 0, LocalPInvoke.SetWindowPosFlags.IgnoreMove | LocalPInvoke.SetWindowPosFlags.IgnoreResize);
                    Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AutoHide, taskbar.TaskbarHwnd);
                    Taskbar.SetTaskbarState(LocalPInvoke.AppBarStates.AlwaysOnTop, taskbar.TaskbarHwnd);
                }
            }
        }

        public TypedEventHandler<ThemeManager, object> TrayIconCheck()
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


        public void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int mt = 0;
            int ml = 0;
            int mb = 0;
            int mr = 0;

            //if (!int.TryParse(cornerRadiusInput.Text, out int roundFactor) || (!int.TryParse(marginInput.Text, out int marginFactor)))
            //{
            //    return;
            //}

            {
                //if (!int.TryParse(mTopInput.Text, out mt) || !int.TryParse(mLeftInput.Text, out ml) || !int.TryParse(mBottomInput.Text, out mb) || !int.TryParse(mRightInput.Text, out mr))
                //{
                //    return;
                //}
            }
            // TODO: Update to incorporate all settings
            //activeSettings.MarginTop = mt;
            //activeSettings.MarginLeft = ml;
            //activeSettings.MarginBottom = mb;
            //activeSettings.MarginRight = mr;
            activeSettings.IsDynamic = (bool)dynamicCheckBox.IsChecked;
            activeSettings.IsCentred = Taskbar.CheckIfCentred();
            activeSettings.ShowTray = (bool)showTrayCheckBox.IsChecked;
            activeSettings.CompositionCompat = (bool)compositionFixCheckBox.IsChecked;
            activeSettings.FillOnMaximise = (bool)fillMaximisedCheckBox.IsChecked;
            activeSettings.FillOnTaskSwitch = (bool)fillAltTabCheckBox.IsChecked;
            activeSettings.ShowTrayOnHover = (bool)showTrayOnHoverCheckBox.IsChecked;

            try
            {
                foreach (Types.Taskbar taskbar in taskbarDetails)
                {
                    int isFullTest = taskbar.TrayRect.Left - taskbar.AppListRect.Right;
                    if (!activeSettings.IsDynamic || (isFullTest <= taskbar.ScaleFactor * 25 && isFullTest > 0 && taskbar.TrayRect.Left != 0))
                    {
                        Taskbar.UpdateSimpleTaskbar(taskbar, activeSettings);
                    }
                    else
                    {
                        Taskbar.UpdateDynamicTaskbar(taskbar, activeSettings);
                    }
                }
            }
            catch (InvalidOperationException aaaa)
            {
                interaction.AddLog(aaaa.Message);
            }


            if (taskbarThread.IsBusy == false)
            {
                taskbarThread.RunWorkerAsync((mt, ml, mb, mr, 0));
            }
            else
            {
                taskbarThread.CancelAsync();
                while (taskbarThread.IsBusy == true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
                taskbarThread.RunWorkerAsync((mt, ml, mb, mr, 0));
            }

            interaction.WriteJSON();
            TrayIconCheck();
            UpdateUi();

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (shouldReallyDieNoReally == false)
            {
                e.Cancel = true;
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show RoundedTB";
            }
            else
            {


                try
                {
                    taskbarThread.CancelAsync();
                }
                catch (Exception aaaa)
                {
                    interaction.AddLog(aaaa.Message);
                }
                while (taskbarThread.IsBusy == true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }

                try
                {
                    foreach (var tbDeets in taskbarDetails)
                    {
                        Taskbar.ResetTaskbar(tbDeets, activeSettings);
                    }
                    if (activeSettings.AutoHide > 0)
                    {
                        AutoHide(false, taskbarDetails);
                    }
                }
                catch (InvalidOperationException aaaa)
                {
                    interaction.AddLog($"Taskbar structure changed on exit:\n{aaaa.Message}");
                }
                interaction.AddLog("Exiting RoundedTB.");
            }
            if (!isAlreadyRunning)
            {
                interaction.WriteJSON();
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Close any popups - leave main window for now
            for (int windowCount = App.Current.Windows.Count - 1; windowCount >= 0; windowCount--)
            {
                App.Current.Windows[windowCount].Close();
            }
            
            shouldReallyDieNoReally = true;

            Close();
        }

        public void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible == false)
            {
                Visibility = Visibility.Visible;
                ShowMenuItem.Header = "Hide RoundedTB";
            }
            else
            {
                // Close any popups - leave main window for now
                for (int windowCount = App.Current.Windows.Count - 1; windowCount >= 0; windowCount--)
                {
                    App.Current.Windows[windowCount].Close();
                }
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show RoundedTB";
            }
        }

        private async void Startup_Clicked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Startup toggled");
            if (IsRunningAsUWP())
            {
                await StartupToggle();
                await StartupInit(false);
            }
            else
            {
                if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk")))
                {
                    System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk"));
                }
                else
                {
                    EnableStartup();
                }
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
                        ShowMenuItem.Header = "Hide RoundedTB";
                    }
                    StartupCheckBox.Content = "Run at startup";
                    break;

                case StartupTaskState.DisabledByUser:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        ShowMenuItem.Header = "Hide RoundedTB";
                    }
                    StartupCheckBox.Content = "Startup unavailable";
                    break;

                case StartupTaskState.EnabledByPolicy:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Hidden;
                        ShowMenuItem.Header = "Show RoundedTB";
                    }
                    StartupCheckBox.Content = "Startup mandatory";
                    break;

                case StartupTaskState.DisabledByPolicy:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = false;
                    if (clean)
                    {
                        Visibility = Visibility.Visible;
                        ShowMenuItem.Header = "Hide RoundedTB";
                    }
                    StartupCheckBox.Content = "Startup unavailable";
                    break;

                case StartupTaskState.Enabled:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = true;
                    if (clean)
                    {
                        Visibility = Visibility.Hidden;
                        ShowMenuItem.Header = "Show RoundedTB";
                    }
                    StartupCheckBox.Content = "Run at startup";
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

        private void DebugMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IntPtr hwndNext = LocalPInvoke.FindWindowExA(taskbarDetails[0].TaskbarHwnd, IntPtr.Zero, "Start", null);
            List<IntPtr> floatingMilkshakesBitsOfTaskbar = new List<IntPtr>();
            floatingMilkshakesBitsOfTaskbar.Add(hwndNext);
            while (true) 
            {
                hwndNext = LocalPInvoke.FindWindowExA(taskbarDetails[0].TaskbarHwnd, hwndNext, null, null);
                if (floatingMilkshakesBitsOfTaskbar.Contains(hwndNext))
                {
                    break;
                }
                floatingMilkshakesBitsOfTaskbar.Add(hwndNext);

            }
            foreach (IntPtr hwnd in floatingMilkshakesBitsOfTaskbar)
            {
                LocalPInvoke.GetWindowRect(hwnd, out LocalPInvoke.RECT rect);
                LocalPInvoke.MoveWindow(hwnd, rect.Left + 50, rect.Top, (rect.Right + 50) - (rect.Left + 50), rect.Bottom - rect.Top, true);
            }
        }

        private async void ContextMenu_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsRunningAsUWP())
            {
                await StartupInit(false);
            }
        }

        private void dynamicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled = true;
            showTrayOnHoverCheckBox.IsEnabled = true;
            showTrayOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled = true;
            showTrayCheckBox.IsChecked = true;
            
            if (!isWindows11)
            {
                splitHelpButton.Visibility = Visibility.Visible;
                if (Opacity > 0.5)
                {
                    splitHelpButton_Click(null, null);
                }
            }

        }

        private void dynamicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

            centredCheckBox.IsEnabled = false;
            centredCheckBox.IsChecked = false;
            showTrayOnHoverCheckBox.IsEnabled = false;
            showTrayOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;
            
            if (!isWindows11)
            {
                splitHelpButton.Visibility = Visibility.Hidden;
            }
        }

        private void marginSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ApplyButton_Click(null, null);
        }

        private void cornerRadiusSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ApplyButton_Click(null, null);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            Debug.WriteLine("AAAAA");
            base.OnSourceInitialized(e);


            IntPtr handle = new WindowInteropHelper(this).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(interaction.HwndHook);
            bool wtf = LocalPInvoke.RegisterHotKey(handle, 9000, 0x8, 0x71);
            Debug.WriteLine("KEY: " + wtf);
            Debug.WriteLine(handle);
            Debug.WriteLine((int)Types.KeyModifier.WinKey);
            Debug.WriteLine(System.Windows.Forms.Keys.J.GetHashCode());
            Visibility = Visibility.Hidden;
            Opacity = 1;
        }

        private void splitHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Infobox ib = new Infobox();
            ib.Title = "RoundedTB - Split mode configuration";
            ib.titleBlock.Text = "How to use Split Mode";
            ib.bodyBlock.Text = "Split mode has a couple of limitations and requires a small amount of setup to get working properly.\n\nLimitations:\n1) Split mode doesn't resize itself automatically. This feature will be coming to RoundedTB for Windows 10 in the future.\n2) Toolbars are not compatible with split mode currently, and will need to be disabled apart from one (more on that in a moment).\n3) Split mode only works when the taskbar is horizontal at the top or bottom of the screen.\n\nSetup:\n1) Right-click the taskbar and disable \"Lock the taskbar\".\n2) Right-click it again and turn off any existing toolbars.\n3) Right-click a third time, select Toolbars > Desktop.\n4) Use the small || handle to resize the taskbar as you please.";
            ib.ShowDialog();
        }

        private void compositionFixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Opacity > 0.01)
            {
                Infobox ib = new Infobox();
                ib.Height = 450;
                ib.Title = "RoundedTB - TranslucentTB compatibility";
                ib.titleBlock.Text = "Compatibility with TranslucentTB";
                ib.bodyBlock.Text = "\nTranslucentTB is a utility that allows you to customise the opacity, blur and colour of the taskbar seamlessly with significantly finer control than other tools. Enable this option to allow RoundedTB and TranslucentTB to work together.\n\nThis is necessary due to a bug in Windows (it's not the fault of RoundedTB or TranslucentTB), and you might encounter some minor flickering when the taskbar \"updates\" (changes size, roundness or position). This is usually pretty minimal and many people use RoundedTB and TranslucentTB in tandem without complaint, but if it bothers you then I recommend sticking with either RoundedTB or TranslucentTB until a better solution is available.\n\nRegardless though, go show TranslucentTB some love! It's the OG Windows 10 aesthetic taskbar mod, the first one on the Microsoft Store and the project that inspired me to make RoundedTB. Plus, the dev is pretty awesome 💖";
                ib.ShowDialog();
            }
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }

        private void fillMaximisedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isWindows11)
            {
                fillAltTabCheckBox.IsEnabled = true;
            }
        }

        private void fillMaximisedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            fillAltTabCheckBox.IsEnabled = false;
            fillAltTabCheckBox.IsChecked = false;

        }

        private void cornerRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cornerRadiusInput.Text = Math.Round(cornerRadiusSlider.Value).ToString();
        }

        private void showTrayOnHoverCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;
        }

        private void showTrayOnHoverCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = true;
            showTrayCheckBox.IsChecked = true;
        }

        private void trayRectStandIn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            trayRectStandIn.Opacity = 1;
        }

        private void trayRectStandIn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (activeSettings.ShowTrayOnHover)
            {
                trayRectStandIn.Opacity = 0.25;
            }
        }

        private void widgetsRectStandIn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            widgetsRectStandIn.Opacity = 1;
        }

        private void widgetsRectStandIn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (activeSettings.ShowTrayOnHover)
            {
                widgetsRectStandIn.Opacity = 0.25;
            }
        }

        private void taskbarRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = WPFUI.Common.Appearance.Primary;
            trayRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            widgetsRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            dynamicCheckBox.Visibility = Visibility.Visible;
            showTrayCheckBox.Visibility = Visibility.Hidden;
            showWidgetsCheckBox.Visibility = Visibility.Hidden;

        }

        private void trayRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            trayRectStandIn.Appearance = WPFUI.Common.Appearance.Primary;
            widgetsRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            dynamicCheckBox.Visibility = Visibility.Hidden;
            showTrayCheckBox.Visibility = Visibility.Visible;
            showWidgetsCheckBox.Visibility = Visibility.Hidden;
        }

        private void widgetsRectStandIn_Click(object sender, RoutedEventArgs e)
        {
            taskbarRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            trayRectStandIn.Appearance = WPFUI.Common.Appearance.Secondary;
            widgetsRectStandIn.Appearance = WPFUI.Common.Appearance.Primary;
            dynamicCheckBox.Visibility = Visibility.Hidden;
            showTrayCheckBox.Visibility = Visibility.Hidden;
            showWidgetsCheckBox.Visibility = Visibility.Visible;
        }
    }
}
