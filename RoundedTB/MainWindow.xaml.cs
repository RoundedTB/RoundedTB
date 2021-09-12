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
        public string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
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
            
            // Check OS build, as behaviours differ between Windows 11 and Windows 10
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var buildNumber = registryKey.GetValue("CurrentBuild").ToString();
            if (Convert.ToInt32(buildNumber) >= 21996)
            {
                isWindows11 = true;
            }
            else
            {
                isWindows11 = false;
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
                    
                }
            }
            bw.DoWork +=bf.DoWork;
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            
            // Load settings into memory/UI
            sf.FileSystem();
            activeSettings = sf.ReadJSON();

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
                    }
                }
            }
            catch (Exception) { }

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
            activeSettings.MarginTop = mt;
            activeSettings.MarginLeft = ml;
            activeSettings.MarginBottom = mb;
            activeSettings.MarginRight = mr;
            activeSettings.IsDynamic = (bool)dynamicCheckBox.IsChecked;
            activeSettings.IsCentred = (bool)centredCheckBox.IsChecked;
            activeSettings.ShowTray = (bool)showTrayCheckBox.IsChecked;
            activeSettings.CompositionCompat = (bool)compositionFixCheckBox.IsChecked;

            foreach (var tbDeets in taskbarDetails)
            {
                bf.UpdateTaskbar(tbDeets, mt, ml, mb, mr, roundFactor, tbDeets.TaskbarRect, activeSettings.IsDynamic, isCentred, activeSettings.ShowTray, 0);
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

            sf.WriteJSON();
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
            if (!isAlreadyRunning)
            {
                sf.WriteJSON();
            }
        }

        // Handles keeping the taskbar updated in the background
        

        

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
                        ShowMenuItem.Header = "Hide RTB";
                    }
                    StartupCheckBox.Content = "Run at startup";
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
                //marginInput.Text = "Dynamic";
                //marginInput.IsEnabled = false;
                Width = 393;
                AdvancedGrid.Visibility = Visibility.Visible;
            }
            else
            {
                //marginInput.Text = "0";
                //marginInput.IsEnabled = true;
                Width = 169;
                AdvancedGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void dynamicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled = true;
            showTrayCheckBox.IsEnabled = true;

        }

        private void dynamicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled = false;
            centredCheckBox.IsChecked = false;

            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;
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
    }
}
