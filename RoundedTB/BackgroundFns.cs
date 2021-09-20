using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;


namespace RoundedTB
{
    public class BackgroundFns
    {
        // Just have a reference point for the Dispatcher
        public MainWindow mw;

        public BackgroundFns()
        {
            mw = (MainWindow)Application.Current.MainWindow;
        }


        // Main method for the BackgroundWorker - runs indefinitely
        public void DoWork(object sender, DoWorkEventArgs e)
        {
            mw.sf.addLog("in bw");
            BackgroundWorker worker = sender as BackgroundWorker;
            while (true)
            {
                try
                {
                    if (worker.CancellationPending == true)
                    {
                        mw.sf.addLog("cancelling");
                        e.Cancel = true;
                        break;
                    }
                    else
                    {
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
                                        mw.isCentred = true;
                                    }
                                    else
                                    {
                                        mw.isCentred = false;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }

                        MonitorStuff.DisplayInfoCollection Displays = MonitorStuff.GetDisplays();
                        for (int a = 0; a < mw.taskbarDetails.Count; a++)
                        {
                            if (!LocalPInvoke.IsWindow(mw.taskbarDetails[a].TaskbarHwnd) || AreThereNewTaskbars(mw.taskbarDetails[a].TaskbarHwnd))
                            {
                                Displays = MonitorStuff.GetDisplays();
                                System.Threading.Thread.Sleep(5000);
                                GenerateTaskbarInfo();
                                mw.numberToForceRefresh = mw.taskbarDetails.Count + 1;
                                goto LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH; // consider this a double-break, it's literally just a few lines below STOP COMPLAINING
                            }
                            IntPtr currentMonitor = LocalPInvoke.MonitorFromWindow(mw.taskbarDetails[a].TaskbarHwnd, 0x2);
                            LocalPInvoke.GetWindowRect(mw.taskbarDetails[a].TaskbarHwnd, out LocalPInvoke.RECT taskbarRectCheck);
                            LocalPInvoke.GetWindowRect(mw.taskbarDetails[a].TrayHwnd, out LocalPInvoke.RECT trayRectCheck);
                            LocalPInvoke.GetWindowRect(mw.taskbarDetails[a].AppListHwnd, out LocalPInvoke.RECT appListRectCheck);

                            // This loop checks for if the taskbar is "hidden" offscreen
                            foreach (MonitorStuff.DisplayInfo Display in Displays)
                            {
                                if (Display.Handle == currentMonitor) // Windows 11, only works when taskbar on bottom but smoother
                                {
                                    bool isVisible = mw.sf.IsTaskbarVisibleOnMonitor(mw.taskbarDetails[a].TaskbarRect, Display.MonitorArea);
                                    if (!isVisible && mw.taskbarDetails[a].Ignored == false)
                                    {
                                        //mw.sf.addLog($"Taskbar {a} was hidden - marking as ignored.");
                                        mw.ResetTaskbar(mw.taskbarDetails[a]);
                                        mw.taskbarDetails[a].Ignored = true;
                                        goto LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH;
                                    }
                                    //if (a == 0)
                                    //{
                                    //    mw.sf.addLog($"TB1-------------------\nVisible: {isVisible}\nBeing ignored by RTB: {mw.taskbarDetails[a].Ignored}");
                                    //}
                                    if (isVisible && mw.taskbarDetails[a].Ignored == true)
                                    {
                                        //mw.sf.addLog($"Taskbar {a} is no longer hidden - removing ignored mark.");
                                        mw.taskbarDetails[a].Ignored = false;
                                    }
                                }
                            }



                            // If the taskbar moves, reset it then restore it
                            if (
                                    taskbarRectCheck.Left != mw.taskbarDetails[a].TaskbarRect.Left ||
                                    taskbarRectCheck.Top != mw.taskbarDetails[a].TaskbarRect.Top ||
                                    taskbarRectCheck.Right != mw.taskbarDetails[a].TaskbarRect.Right ||
                                    taskbarRectCheck.Bottom != mw.taskbarDetails[a].TaskbarRect.Bottom ||

                                    appListRectCheck.Left != mw.taskbarDetails[a].AppListRect.Left ||
                                    appListRectCheck.Top != mw.taskbarDetails[a].AppListRect.Top ||
                                    appListRectCheck.Right != mw.taskbarDetails[a].AppListRect.Right ||
                                    appListRectCheck.Bottom != mw.taskbarDetails[a].AppListRect.Bottom ||

                                    trayRectCheck.Left != mw.taskbarDetails[a].TrayRect.Left ||
                                    trayRectCheck.Top != mw.taskbarDetails[a].TrayRect.Top ||
                                    trayRectCheck.Right != mw.taskbarDetails[a].TrayRect.Right ||
                                    trayRectCheck.Bottom != mw.taskbarDetails[a].TrayRect.Bottom ||

                                    mw.numberToForceRefresh > 0
                              )
                            {
                                int oldWidth = mw.taskbarDetails[a].AppListRect.Right - mw.taskbarDetails[a].TrayRect.Left;
                                Types.Taskbar backupTaskbar = mw.taskbarDetails[a];
                                //ResetTaskbar(mw.taskbarDetails[a]);
                                mw.taskbarDetails[a] = new Types.Taskbar
                                {
                                    TaskbarHwnd = mw.taskbarDetails[a].TaskbarHwnd,
                                    TaskbarRect = taskbarRectCheck,
                                    TrayHwnd = mw.taskbarDetails[a].TrayHwnd,
                                    TrayRect = trayRectCheck,
                                    AppListHwnd = mw.taskbarDetails[a].AppListHwnd,
                                    AppListRect = appListRectCheck,
                                    RecoveryHrgn = mw.taskbarDetails[a].RecoveryHrgn,
                                    ScaleFactor = LocalPInvoke.GetDpiForWindow(mw.taskbarDetails[a].TaskbarHwnd) / 96,
                                    FailCount = mw.taskbarDetails[a].FailCount,
                                    Ignored = false
                                };
                                int newWidth = mw.taskbarDetails[a].AppListRect.Right - mw.taskbarDetails[a].TrayRect.Left;
                                int dynDistChange = Math.Abs(newWidth - oldWidth);

                                //mw.sf.addLog($"Detected taskbar moving! Width changed from [{oldWidth}] to [{newWidth}], total change of {dynDistChange}px");

                                bool failedRefresh = false;
                                SystemFns.Taskbar tbQuery = new SystemFns.Taskbar(mw.taskbarDetails[a].TaskbarHwnd);
                                if (dynDistChange != 0 || tbQuery.AutoHide)
                                {

                                    
                                    try
                                    {
                                        int i1 = (((int, int, int, int, int))e.Argument).Item1;
                                        int i2 = (((int, int, int, int, int))e.Argument).Item2;
                                        int i3 = (((int, int, int, int, int))e.Argument).Item3;
                                        int i4 = (((int, int, int, int, int))e.Argument).Item4;
                                        int i5 = (((int, int, int, int, int))e.Argument).Item5;
                                        mw.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => failedRefresh = UpdateTaskbar(mw.taskbarDetails[a], i1, i2, i3, i4, i5, taskbarRectCheck, mw.activeSettings.IsDynamic, mw.isCentred, mw.activeSettings.ShowTray, dynDistChange)));
                                    }
                                    catch (InvalidCastException)
                                    {
                                        mw.sf.addLog(e.Argument.ToString());
                                        failedRefresh = false;
                                        break;
                                    }

                                }
                                if (!failedRefresh && mw.taskbarDetails[a].FailCount <= 3)
                                {
                                    mw.taskbarDetails[a] = backupTaskbar;
                                    mw.taskbarDetails[a].FailCount++;
                                }
                                else
                                {
                                    mw.taskbarDetails[a].FailCount = 0;
                                }

                                mw.numberToForceRefresh--;
                            }

                            //mw.sf.addLog("Detected taskbar shown");
                            LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH:
                            { };
                        }

                        System.Threading.Thread.Sleep(100);
                    }
                }
                catch (TypeInitializationException ex)
                {
                    mw.sf.addLog(ex.Message);
                    mw.sf.addLog(ex.InnerException.Message);
                    throw ex;
                }
            }
        }

        // Primary code for updating the taskbar regions
        public bool UpdateTaskbar(Types.Taskbar tbDeets, int mTopFactor, int mLeftFactor, int mBottomFactor, int mRightFactor, int roundFactor, LocalPInvoke.RECT rectTaskbarNew, bool isDynamic, bool isCentred, bool showTrayDynamic, int dynChangeDistance)
        {
            if (!tbDeets.Ignored)
            {
                mw.sf.addLog($"Updating taskbar: {tbDeets.TaskbarHwnd}");
                // Basic effective region
                Types.TaskbarEffectiveRegion ter = new Types.TaskbarEffectiveRegion
                {
                    EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                    EffectiveTop = Convert.ToInt32(mTopFactor * tbDeets.ScaleFactor),
                    EffectiveLeft = Convert.ToInt32(mRightFactor * tbDeets.ScaleFactor),
                    EffectiveWidth = Convert.ToInt32(rectTaskbarNew.Right - rectTaskbarNew.Left - (mRightFactor * tbDeets.ScaleFactor)) + 1,
                    EffectiveHeight = Convert.ToInt32(rectTaskbarNew.Bottom - rectTaskbarNew.Top - (mBottomFactor * tbDeets.ScaleFactor)) + 1
                };
                // Dynamic effective region for taskbar
                Types.TaskbarEffectiveRegion dter = new Types.TaskbarEffectiveRegion
                {
                    EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                    EffectiveTop = Convert.ToInt32(mTopFactor * tbDeets.ScaleFactor),
                    EffectiveLeft = Convert.ToInt32(mLeftFactor * tbDeets.ScaleFactor),
                    EffectiveWidth = Convert.ToInt32(rectTaskbarNew.Right - rectTaskbarNew.Left - (mRightFactor * tbDeets.ScaleFactor)) + 1,
                    EffectiveHeight = Convert.ToInt32(rectTaskbarNew.Bottom - rectTaskbarNew.Top - (mBottomFactor * tbDeets.ScaleFactor)) + 1
                };
                // Dynamic effective region for tray
                Types.TaskbarEffectiveRegion tter = new Types.TaskbarEffectiveRegion
                {
                    EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                    EffectiveTop = Convert.ToInt32(mTopFactor * tbDeets.ScaleFactor),
                    EffectiveLeft = Convert.ToInt32(mRightFactor * tbDeets.ScaleFactor),
                    EffectiveWidth = Convert.ToInt32(rectTaskbarNew.Right - rectTaskbarNew.Left - (mLeftFactor * tbDeets.ScaleFactor)) + 1,
                    EffectiveHeight = Convert.ToInt32(rectTaskbarNew.Bottom - rectTaskbarNew.Top - (mBottomFactor * tbDeets.ScaleFactor)) + 1
                };


                if (ter.EffectiveWidth < 48 && isDynamic)
                {
                    mw.sf.addLog($"Taskbar decided to be unreasonably small ({ter.EffectiveWidth}px)");
                    return false;
                }
                if (ter.EffectiveWidth > 10000 & isDynamic)
                {
                    mw.sf.addLog($"Taskbar decided to be unreasonably large ({ter.EffectiveWidth}px)");
                    return false;
                }
                if (!isDynamic)
                {
                    IntPtr rgn = LocalPInvoke.CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
                    LocalPInvoke.SetWindowRgn(tbDeets.TaskbarHwnd, rgn, true);
                    if (mw.activeSettings.CompositionCompat)
                    {
                        SystemFns.UpdateTranslucentTB(tbDeets.TaskbarHwnd);
                    }
                    return true;
                }
                else
                {
                    IntPtr rgn = IntPtr.Zero;
                    IntPtr finalRgn = LocalPInvoke.CreateRoundRectRgn(1, 1, 1, 1, 0, 0);
                    int dynDistance = rectTaskbarNew.Right - tbDeets.AppListRect.Right - Convert.ToInt32(2 * tbDeets.ScaleFactor);

                    if (!mw.isWindows11)
                    {
                        dynDistance -= Convert.ToInt32(20 * tbDeets.ScaleFactor); // If on Windows 10, add an extra 20 logical pixels for the grabhandle
                    }

                    if (dynChangeDistance > (50 * tbDeets.ScaleFactor) && tbDeets.TrayHwnd != IntPtr.Zero && tbDeets.FailCount <= 3)
                    {
                        mw.sf.addLog($"\n----||MESSUP||----\nDYNDIST = {dynDistance}\nDYNCHANGE = {dynChangeDistance}\nTBDIST FROM RIGHT = {rectTaskbarNew.Right}\nAPPLST FROM RIGHT = {tbDeets.AppListRect.Right}\n------------------");
                        return false;
                    }
                    
                    if (tbDeets.TrayHwnd != IntPtr.Zero && tbDeets.AppListRect.Left == 0)
                    {
                        //mw.sf.addLog($"Taskbar is aligned to left: {tbDeets.AppListRect.Left}");
                    }
                    else if (tbDeets.TrayHwnd != IntPtr.Zero)
                    {
                        //mw.sf.addLog($"Taskbar is centred: {tbDeets.AppListRect.Left}");
                    }

                    if (isCentred)
                    {
                        // If the taskbar is centered, take the right-to-right distance off from both sides, as well as the margin
                        rgn = LocalPInvoke.CreateRoundRectRgn(
                            dynDistance + ter.EffectiveLeft,
                            ter.EffectiveTop,
                            ter.EffectiveWidth - dynDistance,
                            ter.EffectiveHeight,
                            ter.EffectiveCornerRadius,
                            ter.EffectiveCornerRadius
                            );
                    }
                    else
                    {
                        // If not, just take it from one side.
                        rgn = LocalPInvoke.CreateRoundRectRgn(
                            dter.EffectiveLeft,
                            dter.EffectiveTop,
                            dter.EffectiveWidth - dynDistance,
                            dter.EffectiveHeight,
                            dter.EffectiveCornerRadius,
                            dter.EffectiveCornerRadius
                            );
                    }

                    if (showTrayDynamic && tbDeets.TrayHwnd != IntPtr.Zero)
                    {
                        IntPtr trayRgn = LocalPInvoke.CreateRoundRectRgn(
                            tbDeets.TrayRect.Left - tter.EffectiveLeft,
                            tter.EffectiveTop,
                            tter.EffectiveWidth,
                            tter.EffectiveHeight,
                            tter.EffectiveCornerRadius,
                            tter.EffectiveCornerRadius
                            );

                        LocalPInvoke.CombineRgn(finalRgn, trayRgn, rgn, 2);
                        rgn = finalRgn;

                    }
                    LocalPInvoke.SetWindowRgn(tbDeets.TaskbarHwnd, rgn, true);
                    if (mw.activeSettings.CompositionCompat)
                    {
                        SystemFns.UpdateTranslucentTB(tbDeets.TaskbarHwnd);
                    }
                    return true;
                }
            }
            return false;

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

        // Checks for new taskbars
        public bool AreThereNewTaskbars(IntPtr checkAfterTaskbar)
        {
            List<IntPtr> currentTaskbars = new List<IntPtr>();
            bool i = true;
            IntPtr hwndPrevious = IntPtr.Zero;
            currentTaskbars.Add(LocalPInvoke.FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_TrayWnd", null));

            while (i)
            {
                IntPtr hwndCurrent = LocalPInvoke.FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_SecondaryTrayWnd", null);
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
            if (currentTaskbars.Count > mw.taskbarDetails.Count)
            {
                return true;
            }
            return false;
        }

        // Generates info about existing taskbars
        public void GenerateTaskbarInfo()
        {
            mw.sf.addLog("#########################\nREGENERATING TASKBAR INFO\n#########################");
            mw.taskbarDetails.Clear(); // Clear taskbar list to start from scratch


            IntPtr hwndMain = LocalPInvoke.FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null); // Find main taskbar
            LocalPInvoke.GetWindowRect(hwndMain, out LocalPInvoke.RECT rectMain); // Get the RECT of the main taskbar
            IntPtr hrgnMain = IntPtr.Zero; // Set recovery region to IntPtr.Zero
            IntPtr hwndTray = LocalPInvoke.FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null); // Get handle to the main taskbar's tray
            LocalPInvoke.GetWindowRect(hwndTray, out LocalPInvoke.RECT rectTray); // Get the RECT for the main taskbar's tray
            IntPtr hwndAppList = LocalPInvoke.FindWindowExA(LocalPInvoke.FindWindowExA(hwndMain, IntPtr.Zero, "ReBarWindow32", null), IntPtr.Zero, "MSTaskSwWClass", null); // Get the handle to the main taskbar's app list
            LocalPInvoke.GetWindowRect(hwndAppList, out LocalPInvoke.RECT rectAppList);// Get the RECT for the main taskbar's app list

            // hwndDesktopButton = FindWindowExA(FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null), IntPtr.Zero, "TrayShowDesktopButtonWClass", null);
            // User32.SetWindowPos(hwndDesktopButton, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_HIDEWINDOW); // Hide "Show Desktop" button

            mw.taskbarDetails.Add(new Types.Taskbar
            {
                TaskbarHwnd = hwndMain,
                TrayHwnd = hwndTray,
                AppListHwnd = hwndAppList,
                TaskbarRect = rectMain,
                TrayRect = rectTray,
                AppListRect = rectAppList,
                RecoveryHrgn = hrgnMain,
                ScaleFactor = Convert.ToDouble(LocalPInvoke.GetDpiForWindow(hwndMain)) / 96.00,
                TaskbarRes = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}",
                FailCount = 0,
                Ignored = false
                // TaskbarEffectWindow = new TaskbarEffect()
            });

            bool i = true;
            IntPtr hwndPrevious = IntPtr.Zero;
            while (i)
            {
                IntPtr hwndCurrent = LocalPInvoke.FindWindowExA(IntPtr.Zero, hwndPrevious, "Shell_SecondaryTrayWnd", null);
                hwndPrevious = hwndCurrent;

                if (hwndCurrent == IntPtr.Zero)
                {
                    i = false;
                }
                else
                {
                    LocalPInvoke.GetWindowRect(hwndCurrent, out LocalPInvoke.RECT rectCurrent);
                    LocalPInvoke.GetWindowRgn(hwndCurrent, out IntPtr hrgnCurrent);
                    IntPtr hwndSecTray = LocalPInvoke.FindWindowExA(hwndCurrent, IntPtr.Zero, "TrayNotifyWnd", null); // Get handle to the main taskbar's tray
                    LocalPInvoke.GetWindowRect(hwndTray, out LocalPInvoke.RECT rectSecTray); // Get the RECT for the main taskbar's tray
                    IntPtr hwndSecAppList = LocalPInvoke.FindWindowExA(LocalPInvoke.FindWindowExA(hwndCurrent, IntPtr.Zero, "WorkerW", null), IntPtr.Zero, "MSTaskListWClass", null); // Get the handle to the main taskbar's app list
                    LocalPInvoke.GetWindowRect(hwndSecAppList, out LocalPInvoke.RECT rectSecAppList);// Get the RECT for the main taskbar's app list
                    mw.taskbarDetails.Add(new Types.Taskbar
                    {
                        TaskbarHwnd = hwndCurrent,
                        TrayHwnd = hwndSecTray,
                        AppListHwnd = hwndSecAppList,
                        TaskbarRect = rectCurrent,
                        TrayRect = rectSecTray,
                        AppListRect = rectSecAppList,
                        RecoveryHrgn = hrgnCurrent,
                        ScaleFactor = Convert.ToDouble(LocalPInvoke.GetDpiForWindow(hwndCurrent)) / 96.00,
                        TaskbarRes = $"{rectCurrent.Right - rectCurrent.Left} x {rectCurrent.Bottom - rectCurrent.Top}",
                        FailCount = 0,
                        Ignored = false
                        // TaskbarEffectWindow = new TaskbarEffect()
                    });
                }
            }

        }
    }
}
