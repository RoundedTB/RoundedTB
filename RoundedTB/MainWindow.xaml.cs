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
using System.Diagnostics;
using Microsoft.Win32;

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
        public int lastDynDistance = 0;
        int numberToForceRefresh = 0;
        public bool isCentred = false;
        public bool isAlreadyRunning = false;


        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public bool? IsOdd(int input)
        {
            decimal comparison = input / 2;
            int check = Convert.ToInt32(comparison) * 2;
            if (check == input)
            {
                return false;
            }
            return true;
        }


        public MainWindow()
        {
            InitializeComponent();
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                shouldReallyDieNoReally = true;
                isAlreadyRunning = true;
                Close();
                return;
            }
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
            
            // Load settings into memory/UI
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
            catch (Exception)
            {

            }

            dynamicCheckBox.IsChecked = activeSettings.IsDynamic;
            centredCheckBox.IsChecked = activeSettings.IsCentred;
            showTrayCheckBox.IsChecked = activeSettings.ShowTray;
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
            activeSettings.MarginTop = mt;
            activeSettings.MarginLeft = ml;
            activeSettings.MarginBottom = mb;
            activeSettings.MarginRight = mr;
            activeSettings.IsDynamic = (bool)dynamicCheckBox.IsChecked;
            activeSettings.IsCentred = (bool)centredCheckBox.IsChecked;
            activeSettings.ShowTray = (bool)showTrayCheckBox.IsChecked;

            foreach (var tbDeets in taskbarDetails)
            {
                UpdateTaskbar(tbDeets, mt, ml, mb, mr, roundFactor, tbDeets.TaskbarRect, activeSettings.IsDynamic, isCentred, activeSettings.ShowTray, 0);
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

            WriteJSON();

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
                WriteJSON();
            }
        }

        // Handles keeping the taskbar updated in the background
        private void Bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("in bw");
            BackgroundWorker worker = sender as BackgroundWorker;
            while (true)
            {
                if (worker.CancellationPending == true)
                {
                    Debug.WriteLine("cancelling");
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
                        GetWindowRect(taskbarDetails[a].TaskbarHwnd, out RECT taskbarRectCheck);
                        GetWindowRect(taskbarDetails[a].TrayHwnd, out RECT trayRectCheck);
                        GetWindowRect(taskbarDetails[a].AppListHwnd, out RECT appListRectCheck);
                        foreach (MonitorStuff.DisplayInfo Display in Displays) // This loop checks for if the taskbar is "hidden" offscreen
                        {
                            if (Display.Handle == currentMonitor)
                            {
                                POINT pt = new POINT { x = taskbarRectCheck.Left + ((taskbarRectCheck.Right - taskbarRectCheck.Left) / 2), y = taskbarRectCheck.Top + ((taskbarRectCheck.Bottom - taskbarRectCheck.Top) / 2) };
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
                        if (
                                taskbarRectCheck.Left != taskbarDetails[a].TaskbarRect.Left ||
                                taskbarRectCheck.Top != taskbarDetails[a].TaskbarRect.Top ||
                                taskbarRectCheck.Right != taskbarDetails[a].TaskbarRect.Right ||
                                taskbarRectCheck.Bottom != taskbarDetails[a].TaskbarRect.Bottom ||

                                appListRectCheck.Left != taskbarDetails[a].AppListRect.Left ||
                                appListRectCheck.Top != taskbarDetails[a].AppListRect.Top ||
                                appListRectCheck.Right != taskbarDetails[a].AppListRect.Right ||
                                appListRectCheck.Bottom != taskbarDetails[a].AppListRect.Bottom ||

                                trayRectCheck.Left != taskbarDetails[a].TrayRect.Left ||
                                trayRectCheck.Top != taskbarDetails[a].TrayRect.Top ||
                                trayRectCheck.Right != taskbarDetails[a].TrayRect.Right ||
                                trayRectCheck.Bottom != taskbarDetails[a].TrayRect.Bottom ||

                                numberToForceRefresh > 0
                          )
                        {
                            int oldWidth = taskbarDetails[a].AppListRect.Right - taskbarDetails[a].TrayRect.Left;
                            Taskbar backupTaskbar = taskbarDetails[a];
                            //Debug.WriteLine("in if");
                            //ResetTaskbar(taskbarDetails[a]);
                            taskbarDetails[a] = new Taskbar
                            {
                                TaskbarHwnd = taskbarDetails[a].TaskbarHwnd,
                                TaskbarRect = taskbarRectCheck,
                                TrayHwnd = taskbarDetails[a].TrayHwnd,
                                TrayRect = trayRectCheck,
                                AppListHwnd = taskbarDetails[a].AppListHwnd,
                                AppListRect = appListRectCheck,
                                RecoveryHrgn = taskbarDetails[a].RecoveryHrgn,
                                ScaleFactor = GetDpiForWindow(taskbarDetails[a].TaskbarHwnd) / 96,
                                FailCount = taskbarDetails[a].FailCount
                            };
                            int newWidth = taskbarDetails[a].AppListRect.Right - taskbarDetails[a].TrayRect.Left;
                            int dynDistChange = Math.Abs(newWidth - oldWidth);

                            bool failedRefresh = false;
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => failedRefresh = UpdateTaskbar(taskbarDetails[a], (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item2, taskbarRectCheck, activeSettings.IsDynamic, isCentred, activeSettings.ShowTray, dynDistChange)));
                            if (!failedRefresh && taskbarDetails[a].FailCount <= 3)
                            {
                                taskbarDetails[a] = backupTaskbar;
                                taskbarDetails[a].FailCount++;
                            }
                            else
                            {
                                taskbarDetails[a].FailCount = 0;
                            }

                            numberToForceRefresh--;
                        }


                    LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH: 
                        { };
                    }
                    
                    // Check if centred
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
                    catch (Exception)
                    {

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
            } // Attempt to close all effect windows - unused
            taskbarDetails.Clear(); // Clear taskbar list to start from scratch


            IntPtr hwndMain = FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null); // Find main taskbar
            GetWindowRect(hwndMain, out RECT rectMain); // Get the RECT of the main taskbar
            IntPtr hrgnMain = IntPtr.Zero; // Set recovery region to IntPtr.Zero
            IntPtr hwndTray = FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null); // Get handle to the main taskbar's tray
            GetWindowRect(hwndTray, out RECT rectTray); // Get the RECT for the main taskbar's tray
            IntPtr hwndAppList = FindWindowExA(FindWindowExA(hwndMain, IntPtr.Zero, "ReBarWindow32", null), IntPtr.Zero, "MSTaskSwWClass", null); // Get the handle to the main taskbar's app list
            GetWindowRect(hwndAppList, out RECT rectAppList);// Get the RECT for the main taskbar's app list

            // hwndDesktopButton = FindWindowExA(FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null), IntPtr.Zero, "TrayShowDesktopButtonWClass", null);
            // User32.SetWindowPos(hwndDesktopButton, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_HIDEWINDOW); // Hide "Show Desktop" button

            taskbarDetails.Add(new Taskbar
            {
                TaskbarHwnd = hwndMain,
                TrayHwnd = hwndTray,
                AppListHwnd = hwndAppList,
                TaskbarRect = rectMain,
                TrayRect = rectTray,
                AppListRect = rectAppList,
                RecoveryHrgn = hrgnMain,
                ScaleFactor = Convert.ToDouble(GetDpiForWindow(hwndMain)) / 96.00,
                TaskbarRes = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}",
                FailCount = 0
                // TaskbarEffectWindow = new TaskbarEffect()
            });

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
                    IntPtr hwndSecTray = FindWindowExA(hwndCurrent, IntPtr.Zero, "TrayNotifyWnd", null); // Get handle to the main taskbar's tray
                    GetWindowRect(hwndTray, out RECT rectSecTray); // Get the RECT for the main taskbar's tray
                    IntPtr hwndSecAppList = FindWindowExA(FindWindowExA(hwndCurrent, IntPtr.Zero, "WorkerW", null), IntPtr.Zero, "MSTaskListWClass", null); // Get the handle to the main taskbar's app list
                    GetWindowRect(hwndSecAppList, out RECT rectSecAppList);// Get the RECT for the main taskbar's app list
                    taskbarDetails.Add(new Taskbar
                    {
                        TaskbarHwnd = hwndCurrent,
                        TrayHwnd = hwndSecTray,
                        AppListHwnd = hwndSecAppList,
                        TaskbarRect = rectCurrent,
                        TrayRect = rectSecTray,
                        AppListRect = rectSecAppList,
                        RecoveryHrgn = hrgnCurrent,
                        ScaleFactor = Convert.ToDouble(GetDpiForWindow(hwndCurrent)) / 96.00,
                        TaskbarRes = $"{rectCurrent.Right - rectCurrent.Left} x {rectCurrent.Bottom - rectCurrent.Top}",
                        FailCount = 0
                        // TaskbarEffectWindow = new TaskbarEffect()
                    });
                }
            }

        }

        public static void ResetTaskbar(Taskbar tbDeets)
        {
            SetWindowRgn(tbDeets.TaskbarHwnd, tbDeets.RecoveryHrgn, true);
        }

        public static bool UpdateTaskbar(Taskbar tbDeets, int mTopFactor, int mLeftFactor, int mBottomFactor, int mRightFactor, int roundFactor, RECT rectTaskbarNew, bool isDynamic, bool isCentred, bool showTrayDynamic, int dynChangeDistance)
        {
            TaskbarEffectiveRegion ter = new TaskbarEffectiveRegion
            {
                EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                EffectiveTop = Convert.ToInt32(mTopFactor * tbDeets.ScaleFactor),
                EffectiveLeft = Convert.ToInt32(mLeftFactor * tbDeets.ScaleFactor),
                EffectiveWidth = Convert.ToInt32(rectTaskbarNew.Right - rectTaskbarNew.Left - (mRightFactor * tbDeets.ScaleFactor)) + 1,
                EffectiveHeight = Convert.ToInt32(rectTaskbarNew.Bottom - rectTaskbarNew.Top - (mBottomFactor * tbDeets.ScaleFactor)) + 1
            };

            if (!isDynamic)
            {
                IntPtr rgn = CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
                SetWindowRgn(tbDeets.TaskbarHwnd, rgn, true);
                SendMessage(tbDeets.TaskbarHwnd, 798, 0, IntPtr.Zero); // TTB compat
                return true;
            }
            else
            {
                IntPtr rgn = IntPtr.Zero;
                IntPtr finalRgn = CreateRoundRectRgn(1, 1, 1, 1, 0, 0);
                int dynDistance = rectTaskbarNew.Right - tbDeets.AppListRect.Right - Convert.ToInt32(2 * tbDeets.ScaleFactor);
                if (dynChangeDistance > (50 * tbDeets.ScaleFactor) && tbDeets.TrayHwnd != IntPtr.Zero && tbDeets.FailCount <= 3)
                {
                    Debug.WriteLine($"----||FUCKUP||----");
                    Debug.WriteLine($"DYNDIST = {dynDistance}");
                    Debug.WriteLine($"DYNCHANGE = {dynChangeDistance}");
                    Debug.WriteLine($"TBDIST FROM RIGHT = {rectTaskbarNew.Right}");
                    Debug.WriteLine($"APPLST FROM RIGHT = {tbDeets.AppListRect.Right}");
                    Debug.WriteLine($"------------------");
                    return false;
                }
                if (tbDeets.TrayHwnd != IntPtr.Zero && tbDeets.AppListRect.Left == 0)
                {
                    Debug.WriteLine($"Taskbar is aligned to left: {tbDeets.AppListRect.Left}");
                }
                else if (tbDeets.TrayHwnd != IntPtr.Zero)
                {
                        Debug.WriteLine($"Taskbar is centred: {tbDeets.AppListRect.Left}");
                }

                if (isCentred)
                {
                    // If the taskbar is centered, take the right-to-right distance off from both sides, as well as the margin
                    rgn = CreateRoundRectRgn(dynDistance + ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth - dynDistance, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
                }
                else
                {
                    // If not, just take it from one side.
                    rgn = CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth - dynDistance, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
                }

                if (showTrayDynamic && tbDeets.TrayHwnd != IntPtr.Zero)
                {
                    IntPtr trayRgn = CreateRoundRectRgn(tbDeets.TrayRect.Left - ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);

                    CombineRgn(finalRgn, trayRgn, rgn, 2);
                    rgn = finalRgn;

                }
                SetWindowRgn(tbDeets.TaskbarHwnd, rgn, true);
                IntPtr a = SendMessage(tbDeets.TaskbarHwnd, 798, 1, IntPtr.Zero); // TTB compat
                bool b = RedrawWindow(tbDeets.TaskbarHwnd, IntPtr.Zero, IntPtr.Zero, RedrawWindowFlags.Erase | RedrawWindowFlags.Invalidate | RedrawWindowFlags.Frame | RedrawWindowFlags.UpdateNow);
                Debug.WriteLine($"SendMessage returned: {a}");
                Debug.WriteLine($"RedrawWindow returned: {b}");
                return true;
            }


            // IntPtr effectHandle = new WindowInteropHelper(tbDeets.TaskbarEffectWindow).Handle;
            //GetWindowRect(FindWindowExA(FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null), IntPtr.Zero, "TrayNotifyWnd", null), out RECT trayRect);
            //IntPtr trayRgn = CreateRoundRectRgn(trayRect.Left - ter.EffectiveTop, ter.EffectiveTop, trayRect.Right - ter.EffectiveTop, (trayRect.Bottom - trayRect.Top) - ter.EffectiveTop, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
            //IntPtr tbRgn = CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveRight, ter.EffectiveBottom, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
            //IntPtr finalRgn = CreateRoundRectRgn(1,1,1,1,0,0);
            //CombineRgn(finalRgn, trayRgn, tbRgn, 2);
            //SetWindowRgn(tbDeets.TaskbarHwnd, finalRgn, true);
            // SetWindowRgn(effectHandle, CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveRight, ter.EffectiveBottom, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius), true);
            // MoveWindow(effectHandle, rectNew.Left, rectNew.Top, rectNew.Right - rectNew.Left, rectNew.Bottom - rectNew.Top, true);
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
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr idk, IntPtr hrgnUpdate, RedrawWindowFlags flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

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
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [Flags()]
        private enum RedrawWindowFlags : uint
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

        public class Taskbar
        {
            public IntPtr TaskbarHwnd { get; set; } // Handle to the taskbar
            public IntPtr TrayHwnd { get; set; } // Handle to the tray on the taskbar (if present)
            public IntPtr AppListHwnd { get; set; } // Handle to the list of open/pinned apps on the taskbar
            public RECT TaskbarRect { get; set; } // Bounding box for the taskbar
            public RECT TrayRect { get; set; }  // Bounding box for the tray (dynamic)
            public RECT AppListRect { get; set; } // Bounding box for the list of pinned & open apps (dynamic)
            public IntPtr RecoveryHrgn { get; set; } // Pointer to the recovery region for any given taskbar. Defaults to IntPtr.Zero
            public double ScaleFactor { get; set; } // The scale factor of the monitor the taskbar is on
            public string TaskbarRes { get; set; } // Resolution of the taskbar as text
            public int FailCount { get; set; } // Number of times the taskbar has had an "erroneous" size at applytime

            public int AppListWidth { get; set; }
            public TaskbarEffect TaskbarEffectWindow { get; set; }
        }

        public class Settings
        {
            public int CornerRadius { get; set; }
            public int MarginBottom { get; set; }
            public int MarginLeft { get; set; }
            public int MarginRight { get; set; }
            public int MarginTop { get; set; }
            public bool IsDynamic { get; set; }
            public bool IsCentred { get; set; }
            public bool ShowTray { get; set; }
        }

        public class TaskbarEffectiveRegion
        {
            public int EffectiveCornerRadius { get; set; }
            public int EffectiveTop { get; set; }
            public int EffectiveLeft { get; set; }
            public int EffectiveWidth { get; set; }
            public int EffectiveHeight { get; set; }
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
    }
}
