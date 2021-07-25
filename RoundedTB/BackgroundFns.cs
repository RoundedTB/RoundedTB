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
        public static MainWindow mw = (MainWindow)Application.Current.MainWindow;

        public static void DoWork(object sender, DoWorkEventArgs e)
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
                    for (int a = 0; a < MainWindow.taskbarDetails.Count; a++)
                    {
                        if (!LocalPInvoke.IsWindow(MainWindow.taskbarDetails[a].TaskbarHwnd) || AreThereNewTaskbars(MainWindow.taskbarDetails[a].TaskbarHwnd))
                        {
                            Displays = MonitorStuff.GetDisplays();
                            System.Threading.Thread.Sleep(5000);
                            GenerateTaskbarInfo();
                            MainWindow.numberToForceRefresh = MainWindow.taskbarDetails.Count + 1;
                            goto LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH; // consider this a double-break, it's literally just a few lines below STOP COMPLAINING
                        }

                        IntPtr currentMonitor = LocalPInvoke.MonitorFromWindow(MainWindow.taskbarDetails[a].TaskbarHwnd, 0x2);
                        LocalPInvoke.GetWindowRect(MainWindow.taskbarDetails[a].TaskbarHwnd, out LocalPInvoke.RECT taskbarRectCheck);
                        LocalPInvoke.GetWindowRect(MainWindow.taskbarDetails[a].TrayHwnd, out LocalPInvoke.RECT trayRectCheck);
                        LocalPInvoke.GetWindowRect(MainWindow.taskbarDetails[a].AppListHwnd, out LocalPInvoke.RECT appListRectCheck);
                        foreach (MonitorStuff.DisplayInfo Display in Displays) // This loop checks for if the taskbar is "hidden" offscreen
                        {
                            if (Display.Handle == currentMonitor)
                            {
                                LocalPInvoke.POINT pt = new LocalPInvoke.POINT { x = taskbarRectCheck.Left + ((taskbarRectCheck.Right - taskbarRectCheck.Left) / 2), y = taskbarRectCheck.Top + ((taskbarRectCheck.Bottom - taskbarRectCheck.Top) / 2) };
                                LocalPInvoke.RECT refRect = Display.MonitorArea;
                                bool isOnTaskbar = LocalPInvoke.PtInRect(ref refRect, pt);
                                if (!isOnTaskbar)
                                {
                                    MainWindow.ResetTaskbar(MainWindow.taskbarDetails[a]);
                                    goto LiterallyJustGoingDownToTheEndOfThisLoopStopHavingAHissyFitSMFH; // consider this a double-break, it's literally just a few lines below STOP COMPLAINING
                                }
                            }
                        }



                        // If the taskbar moves, reset it then restore it
                        if (
                                taskbarRectCheck.Left != MainWindow.taskbarDetails[a].TaskbarRect.Left ||
                                taskbarRectCheck.Top != MainWindow.taskbarDetails[a].TaskbarRect.Top ||
                                taskbarRectCheck.Right != MainWindow.taskbarDetails[a].TaskbarRect.Right ||
                                taskbarRectCheck.Bottom != MainWindow.taskbarDetails[a].TaskbarRect.Bottom ||

                                appListRectCheck.Left != MainWindow.taskbarDetails[a].AppListRect.Left ||
                                appListRectCheck.Top != MainWindow.taskbarDetails[a].AppListRect.Top ||
                                appListRectCheck.Right != MainWindow.taskbarDetails[a].AppListRect.Right ||
                                appListRectCheck.Bottom != MainWindow.taskbarDetails[a].AppListRect.Bottom ||

                                trayRectCheck.Left != MainWindow.taskbarDetails[a].TrayRect.Left ||
                                trayRectCheck.Top != MainWindow.taskbarDetails[a].TrayRect.Top ||
                                trayRectCheck.Right != MainWindow.taskbarDetails[a].TrayRect.Right ||
                                trayRectCheck.Bottom != MainWindow.taskbarDetails[a].TrayRect.Bottom ||

                                MainWindow.numberToForceRefresh > 0
                          )
                        {
                            int oldWidth = MainWindow.taskbarDetails[a].AppListRect.Right - MainWindow.taskbarDetails[a].TrayRect.Left;
                            Types.Taskbar backupTaskbar = MainWindow.taskbarDetails[a];
                            //Debug.WriteLine("in if");
                            //ResetTaskbar(MainWindow.taskbarDetails[a]);
                            MainWindow.taskbarDetails[a] = new Types.Taskbar
                            {
                                TaskbarHwnd = MainWindow.taskbarDetails[a].TaskbarHwnd,
                                TaskbarRect = taskbarRectCheck,
                                TrayHwnd = MainWindow.taskbarDetails[a].TrayHwnd,
                                TrayRect = trayRectCheck,
                                AppListHwnd = MainWindow.taskbarDetails[a].AppListHwnd,
                                AppListRect = appListRectCheck,
                                RecoveryHrgn = MainWindow.taskbarDetails[a].RecoveryHrgn,
                                ScaleFactor = LocalPInvoke.GetDpiForWindow(MainWindow.taskbarDetails[a].TaskbarHwnd) / 96,
                                FailCount = MainWindow.taskbarDetails[a].FailCount
                            };
                            int newWidth = MainWindow.taskbarDetails[a].AppListRect.Right - MainWindow.taskbarDetails[a].TrayRect.Left;
                            int dynDistChange = Math.Abs(newWidth - oldWidth);

                            bool failedRefresh = false;
                            mw.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => failedRefresh = UpdateTaskbar(MainWindow.taskbarDetails[a], (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item2, taskbarRectCheck, MainWindow.activeSettings.IsDynamic, MainWindow.isCentred, MainWindow.activeSettings.ShowTray, dynDistChange)));
                            if (!failedRefresh && MainWindow.taskbarDetails[a].FailCount <= 3)
                            {
                                MainWindow.taskbarDetails[a] = backupTaskbar;
                                MainWindow.taskbarDetails[a].FailCount++;
                            }
                            else
                            {
                                MainWindow.taskbarDetails[a].FailCount = 0;
                            }

                            MainWindow.numberToForceRefresh--;
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
                                    MainWindow.isCentred = true;
                                }
                                else
                                {
                                    MainWindow.isCentred = false;
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

        public static bool UpdateTaskbar(Types.Taskbar tbDeets, int mTopFactor, int mLeftFactor, int mBottomFactor, int mRightFactor, int roundFactor, LocalPInvoke.RECT rectTaskbarNew, bool isDynamic, bool isCentred, bool showTrayDynamic, int dynChangeDistance)
        {
            Types.TaskbarEffectiveRegion ter = new Types.TaskbarEffectiveRegion
            {
                EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                EffectiveTop = Convert.ToInt32(mTopFactor * tbDeets.ScaleFactor),
                EffectiveLeft = Convert.ToInt32(mLeftFactor * tbDeets.ScaleFactor),
                EffectiveWidth = Convert.ToInt32(rectTaskbarNew.Right - rectTaskbarNew.Left - (mRightFactor * tbDeets.ScaleFactor)) + 1,
                EffectiveHeight = Convert.ToInt32(rectTaskbarNew.Bottom - rectTaskbarNew.Top - (mBottomFactor * tbDeets.ScaleFactor)) + 1
            };

            if (!isDynamic)
            {
                IntPtr rgn = LocalPInvoke.CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
                LocalPInvoke.SetWindowRgn(tbDeets.TaskbarHwnd, rgn, true);
                LocalPInvoke.SendMessage(tbDeets.TaskbarHwnd, 798, 0, IntPtr.Zero); // TTB compat
                return true;
            }
            else
            {
                IntPtr rgn = IntPtr.Zero;
                IntPtr finalRgn = LocalPInvoke.CreateRoundRectRgn(1, 1, 1, 1, 0, 0);
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
                    rgn = LocalPInvoke.CreateRoundRectRgn(dynDistance + ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth - dynDistance, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
                }
                else
                {
                    // If not, just take it from one side.
                    rgn = LocalPInvoke.CreateRoundRectRgn(ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth - dynDistance, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);
                }

                if (showTrayDynamic && tbDeets.TrayHwnd != IntPtr.Zero)
                {
                    IntPtr trayRgn = LocalPInvoke.CreateRoundRectRgn(tbDeets.TrayRect.Left - ter.EffectiveLeft, ter.EffectiveTop, ter.EffectiveWidth, ter.EffectiveHeight, ter.EffectiveCornerRadius, ter.EffectiveCornerRadius);

                    LocalPInvoke.CombineRgn(finalRgn, trayRgn, rgn, 2);
                    rgn = finalRgn;

                }
                LocalPInvoke.SetWindowRgn(tbDeets.TaskbarHwnd, rgn, true);
                IntPtr a = LocalPInvoke.SendMessage(tbDeets.TaskbarHwnd, 798, 1, IntPtr.Zero); // TTB compat
                bool b = LocalPInvoke.RedrawWindow(tbDeets.TaskbarHwnd, IntPtr.Zero, IntPtr.Zero, LocalPInvoke.RedrawWindowFlags.Erase | LocalPInvoke.RedrawWindowFlags.Invalidate | LocalPInvoke.RedrawWindowFlags.Frame | LocalPInvoke.RedrawWindowFlags.UpdateNow);
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

        public static bool AreThereNewTaskbars(IntPtr checkAfterTaskbar)
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

            if (currentTaskbars.Count > MainWindow.taskbarDetails.Count)
            {
                return true;
            }
            return false;
        }

        public static void GenerateTaskbarInfo()
        {
            foreach (Types.Taskbar tb in MainWindow.taskbarDetails)
            {
                try
                {
                    //tb.TaskbarEffectWindow.Close();
                }
                catch (Exception) { }
            } // Attempt to close all effect windows - unused
            MainWindow.taskbarDetails.Clear(); // Clear taskbar list to start from scratch


            IntPtr hwndMain = LocalPInvoke.FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null); // Find main taskbar
            LocalPInvoke.GetWindowRect(hwndMain, out LocalPInvoke.RECT rectMain); // Get the RECT of the main taskbar
            IntPtr hrgnMain = IntPtr.Zero; // Set recovery region to IntPtr.Zero
            IntPtr hwndTray = LocalPInvoke.FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null); // Get handle to the main taskbar's tray
            LocalPInvoke.GetWindowRect(hwndTray, out LocalPInvoke.RECT rectTray); // Get the RECT for the main taskbar's tray
            IntPtr hwndAppList = LocalPInvoke.FindWindowExA(LocalPInvoke.FindWindowExA(hwndMain, IntPtr.Zero, "ReBarWindow32", null), IntPtr.Zero, "MSTaskSwWClass", null); // Get the handle to the main taskbar's app list
            LocalPInvoke.GetWindowRect(hwndAppList, out LocalPInvoke.RECT rectAppList);// Get the RECT for the main taskbar's app list

            // hwndDesktopButton = FindWindowExA(FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null), IntPtr.Zero, "TrayShowDesktopButtonWClass", null);
            // User32.SetWindowPos(hwndDesktopButton, IntPtr.Zero, 0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_HIDEWINDOW); // Hide "Show Desktop" button

            MainWindow.taskbarDetails.Add(new Types.Taskbar
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
                FailCount = 0
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
                    MainWindow.taskbarDetails.Add(new Types.Taskbar
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
                        FailCount = 0
                        // TaskbarEffectWindow = new TaskbarEffect()
                    });
                }
            }

        }
    }
}
