using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

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
            TaskbarEffectiveRegion ter; // TODO - Work out what on earth is going on here.
            if (tbDeets.ScaleFactor >= 1.5)
            {
                ter = new TaskbarEffectiveRegion
                {
                    EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                    EffectiveTopLeft = Convert.ToInt32(marginFactor * tbDeets.ScaleFactor),
                    EffectiveBottomRightX = Convert.ToInt32(rectNew.Right - rectNew.Left + 1) - marginFactor,
                    EffectiveBottomRightY = Convert.ToInt32(rectNew.Bottom - rectNew.Top + 1) - marginFactor
                };
            }
            else
            {
                ter = new TaskbarEffectiveRegion
                {
                    EffectiveCornerRadius = Convert.ToInt32(roundFactor * tbDeets.ScaleFactor),
                    EffectiveTopLeft = Convert.ToInt32(marginFactor * tbDeets.ScaleFactor),
                    EffectiveBottomRightX = Convert.ToInt32(rectNew.Right - rectNew.Left) - marginFactor,
                    EffectiveBottomRightY = Convert.ToInt32(rectNew.Bottom - rectNew.Top) - marginFactor
                };
            }


            SetWindowRgn(tbDeets.TaskbarHwnd, CreateRoundRectRgn(ter.EffectiveTopLeft, ter.EffectiveTopLeft , ter.EffectiveBottomRightX, ter.EffectiveBottomRightY , ter.EffectiveCornerRadius, ter.EffectiveCornerRadius), true);
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
    }
}
