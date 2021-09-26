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

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool isWindows11;
        public List<Types.Taskbar> taskbarDetails = new List<Types.Taskbar>();
        public bool shouldReallyDieNoReally = false;
        //public string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.json");
        public string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rtb.log");
        public Types.Settings activeSettings = new Types.Settings();
        public BackgroundWorker bw = new BackgroundWorker();
        public IntPtr hwndDesktopButton = IntPtr.Zero;
        public int lastDynDistance = 0;
        public int numberToForceRefresh = 0;
        public bool isCentred = false;
        public bool isAlreadyRunning = false;
        public BackgroundFns bf;
        public SystemFns sf;
        private HwndSource source;
        public bool preview = false; // Controls whether or not to compile a preview build - janky way of doing it but hey



        public MainWindow()
        {
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
                dynamicCheckBox.Content = "Split mode";
            }

            // Initialise functions
            bf = new BackgroundFns();
            sf = new SystemFns();
            
            // Check if RoundedTB is already running, and if it is, do nothing.
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                shouldReallyDieNoReally = true;
                isAlreadyRunning = true;
                MessageBox.Show("Only one instance of RoundedTB can be run at once.", "RoundedTB", MessageBoxButton.OK);
                Close();
                return;
            }
            TrayIconCheck();

            if (IsRunningAsUWP())
            {
                #pragma warning disable CS4014
                StartupInit(true);
                //localFolder = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
                configPath = Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "rtb.json");
                logPath = Path.Combine(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "rtb.log");
            }
            if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk")) && !IsRunningAsUWP())
            {
                StartupCheckBox.IsChecked = true;
                ShowMenuItem.Header = "Show RoundedTB";
            }
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            bw.DoWork +=bf.DoWork;

            // Load settings into memory/UI
            sf.FileSystem();
            if (!IsRunningAsUWP())
            {
                sf.addLog($"RoundedTB started!");
            }
            else
            {
                sf.addLog($"RoundedTB started in UWP mode!");
            }
            activeSettings = sf.ReadJSON();
            if (activeSettings == null)
            {
                if (isWindows11)
                {
                    activeSettings = new Types.Settings()
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
                    activeSettings = new Types.Settings()
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
            }
            sf.addLog($"Settings loaded:");
            sf.addLog(
                $"\nCornerRadius: {activeSettings.CornerRadius}\n" +
                $"MarginBasic: {activeSettings.MarginBasic}\n" +
                $"MarginBottom: {activeSettings.MarginBottom}\n" +
                $"MarginLeft: {activeSettings.MarginLeft}\n" +
                $"MarginRight: {activeSettings.MarginRight}\n" +
                $"MarginTop: {activeSettings.MarginTop}\n" +
                $"IsDynamic: {activeSettings.IsDynamic}\n" +
                $"IsCentred: {activeSettings.IsCentred}\n" +
                $"ShowTray: {activeSettings.ShowTray}\n" +
                $"CompositionCompat: {activeSettings.CompositionCompat}\n" +
                $"IsNotFirstLaunch: {activeSettings.IsNotFirstLaunch}\n"
                );
            if (activeSettings.MarginBasic == -384)
            {
                marginInput.Text = "Advanced";
                marginSlider.IsEnabled = false;
                marginInput.IsEnabled = false;
                mTopInput.IsEnabled = true;
                mLeftInput.IsEnabled = true;
                mBottomInput.IsEnabled = true;
                mRightInput.IsEnabled = true;

                mTopInput.Text = activeSettings.MarginTop.ToString();
                mLeftInput.Text = activeSettings.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.MarginBottom.ToString();
                mRightInput.Text = activeSettings.MarginRight.ToString();

            }
            else
            {
                marginInput.Text = activeSettings.MarginBasic.ToString();
                marginSlider.IsEnabled = true;
                marginInput.IsEnabled = true;
                mTopInput.IsEnabled = false;
                mLeftInput.IsEnabled = false;
                mBottomInput.IsEnabled = false;
                mRightInput.IsEnabled = false;
            }

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
                        sf.addLog($"Taskbar centred? {isCentred}");
                    }
                }
            }
            catch (Exception aaaa)
            {
                sf.addLog(aaaa.Message);
            }

            dynamicCheckBox.IsChecked = activeSettings.IsDynamic;
            centredCheckBox.IsChecked = activeSettings.IsCentred;
            showTrayCheckBox.IsChecked = activeSettings.ShowTray;
            compositionFixCheckBox.IsChecked = activeSettings.CompositionCompat;
            cornerRadiusInput.Text = activeSettings.CornerRadius.ToString();
            bf.GenerateTaskbarInfo();
            if (marginInput.Text != null && cornerRadiusInput.Text != null)
            {
                ApplyButton_Click(null, null);
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

            if (preview) 
            {
                MessageBox.Show("This is an unreleased preview build of RoundedTB!\n\nThings are likely horribly broken." +
                    "This build is not ready for normal daily use. As such, this message box will appear every time you launch the app," +
                    "and the startup checkbox has been disabled.", "RoundedTB Dev");
                StartupCheckBox.IsEnabled = false;
                StartupCheckBox.Content = "Preview build";
                try
                {
                    System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "RoundedTB.lnk"));
                }
                catch (Exception) { }
                Title = "RoundedTB Preview";
                previewWarningLabel.Visibility = Visibility.Visible;
                Visibility = Visibility.Visible;
            }
            if (activeSettings.IsNotFirstLaunch != true)
            {
                activeSettings.IsNotFirstLaunch = true;
                AboutWindow aw = new AboutWindow();
                aw.expander0.IsExpanded = true;
                aw.ShowDialog();
                Visibility = Visibility.Visible;
                ShowMenuItem.Header = "Hide RoundedTB";
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

        public void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            int mt = 0;
            int ml = 0;
            int mb = 0;
            int mr = 0;

            if (!int.TryParse(cornerRadiusInput.Text, out int roundFactor) || (!int.TryParse(marginInput.Text, out int marginFactor) && activeSettings.MarginBasic != -384))
            {
                return;
            }

            activeSettings.CornerRadius = roundFactor;
            if (marginInput.IsEnabled)
            {
                mt = marginFactor;
                ml = marginFactor;
                mb = marginFactor;
                mr = marginFactor;
                activeSettings.MarginBasic = marginFactor;
            }
            else
            {
                if (!int.TryParse(mTopInput.Text, out mt) || !int.TryParse(mLeftInput.Text, out ml) || !int.TryParse(mBottomInput.Text, out mb) || !int.TryParse(mRightInput.Text, out mr))
                {
                    return;
                }
                activeSettings.MarginBasic = -384;
            }
            activeSettings.MarginTop = mt;
            activeSettings.MarginLeft = ml;
            activeSettings.MarginBottom = mb;
            activeSettings.MarginRight = mr;
            activeSettings.IsDynamic = (bool)dynamicCheckBox.IsChecked;
            activeSettings.IsCentred = (bool)centredCheckBox.IsChecked;
            activeSettings.ShowTray = (bool)showTrayCheckBox.IsChecked;
            activeSettings.CompositionCompat = (bool)compositionFixCheckBox.IsChecked;

            try
            {
                foreach (var tbDeets in taskbarDetails)
                {
                    bf.UpdateTaskbar(tbDeets, mt, ml, mb, mr, roundFactor, tbDeets.TaskbarRect, activeSettings.IsDynamic, isCentred, activeSettings.ShowTray, 0);
                }
            }
            catch (InvalidOperationException aaaa)
            {
                sf.addLog(aaaa.Message);
            }


            if (bw.IsBusy == false)
            {
                bw.RunWorkerAsync((mt, ml, mb, mr, roundFactor));
            }
            else
            {
                bw.CancelAsync();
                while (bw.IsBusy == true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
                bw.RunWorkerAsync((mt, ml, mb, mr, roundFactor));
            }

            sf.WriteJSON();

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
                    bw.CancelAsync();
                }
                catch (Exception aaaa)
                {
                    sf.addLog(aaaa.Message);
                }
                while (bw.IsBusy == true)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
                sf.addLog("Exiting RoundedTB.");
            }
            if (!isAlreadyRunning)
            {
                sf.WriteJSON();
            }
        }

        // Handles resetting the taskbar
        public void ResetTaskbar(Types.Taskbar tbDeets)
        {
            LocalPInvoke.SetWindowRgn(tbDeets.TaskbarHwnd, tbDeets.RecoveryHrgn, true);
            if (activeSettings.CompositionCompat)
            {
                SystemFns.UpdateTranslucentTB(tbDeets.TaskbarHwnd);
            }
        }

        



        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            shouldReallyDieNoReally = true;
            try
            {
                foreach (var tbDeets in taskbarDetails)
                {
                    ResetTaskbar(tbDeets);
                }
            }
            catch (InvalidOperationException aaaa)
            {
                sf.addLog($"Taskbar structure changed on exit:\n{aaaa.Message}");
            }


            Close();
        }

        private void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsVisible == false)
            {
                Visibility = Visibility.Visible;
                ShowMenuItem.Header = "Hide RoundedTB";
            }
            else
            {
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show RoundedTB";
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



        public void EnableStartup()
        {
            if (preview)
            {
                return;
            }
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
            List<IntPtr> bitsOfTaskbar = new List<IntPtr>();
            bitsOfTaskbar.Add(hwndNext);
            while (true) 
            {
                hwndNext = LocalPInvoke.FindWindowExA(taskbarDetails[0].TaskbarHwnd, hwndNext, null, null);
                if (bitsOfTaskbar.Contains(hwndNext))
                {
                    break;
                }
                bitsOfTaskbar.Add(hwndNext);

            }
            foreach (IntPtr hwnd in bitsOfTaskbar)
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

        private void advancedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Width < 300)
            {
                Width = 393;
                AdvancedGrid.Visibility = Visibility.Visible;
                advancedMarginsButton.Visibility = Visibility.Visible;
            }
            else
            {
                Width = 169;
                AdvancedGrid.Visibility = Visibility.Collapsed;
                advancedMarginsButton.Visibility = Visibility.Hidden;
            }
        }

        private void dynamicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled = true;
            showTrayCheckBox.IsEnabled = true;
            mLeftLabel.Content = "Outer Margin";
            mRightLabel.Content = "Inner Margin";

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
            mLeftLabel.Content = "Left Margin";
            mRightLabel.Content = "Right Margin";

            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;
            
            if (!isWindows11)
            {
                splitHelpButton.Visibility = Visibility.Hidden;
            }
        }

        private void marginSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            marginInput.Text = Math.Round(marginSlider.Value).ToString();
        }

        private void marginSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ApplyButton_Click(null, null);
        }

        private void cornerRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cornerRadiusInput.Text = Math.Round(cornerRadiusSlider.Value).ToString();
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
            source.AddHook(sf.HwndHook);
            //bool wtf = LocalPInvoke.RegisterHotKey(handle, 9000, (int)Types.KeyModifier.WinKey, System.Windows.Forms.Keys.J.GetHashCode());
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

        private void advancedMarginsButton_Click(object sender, RoutedEventArgs e)
        {
            if (marginInput.IsEnabled)
            {
                marginInput.Text = "Advanced";
                activeSettings.MarginBasic = -384;
                marginSlider.Value = 0;
                marginSlider.IsEnabled = false;
                marginInput.IsEnabled = false;
                mTopInput.IsEnabled = true;
                mLeftInput.IsEnabled = true;
                mBottomInput.IsEnabled = true;
                mRightInput.IsEnabled = true;
            }
            else
            {
                marginInput.Text = "0";
                activeSettings.MarginBasic = 0;
                marginSlider.IsEnabled = true;
                marginInput.IsEnabled = true;
                mTopInput.IsEnabled = false;
                mLeftInput.IsEnabled = false;
                mBottomInput.IsEnabled = false;
                mRightInput.IsEnabled = false;
            }
        }

        private void compositionFixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Opacity > 0.01)
            {
                Infobox ib = new Infobox();
                ib.Height = 450;
                ib.Title = "RoundedTB - TranslucentTB compatibility";
                ib.titleBlock.Text = "Compatibility with TranslucentTB";
                ib.bodyBlock.Text = "TranslucentTB is an awesome project that inspired me to make RoundedTB. It allows you to customise the opacity, blur and colour of the taskbar seamlessly with significantly finer control than anything else. Unfortunately, due to a bug with Windows' Desktop Window Manager (DWM), RoundedTB and TranslucentTB don't work properly together at the moment.\n\nThis bug is not the fault of RoundedTB or TranslucentTB, and I'm working closely with TranslucentTB's sole developer to find a proper solution. Until then however, this compatibility mitigation exists. For this to work, you will need TranslucentTB version 2021.5 or higher.\n\nIf this option is enabled with a compatible version of TranslucentTB, you should see TranslucentTB's effects and colours apply correctly. However, there may be significant flickering when the taskbar moves or resizes.\n\nAs soon as I find a proper workaround to Windows' DWM bug, it will be implemented and this option will be removed. Until then, go show TranslucentTB some love! 💖";
                ib.ShowDialog();
            }
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }
    }
}
