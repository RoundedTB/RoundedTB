using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
//using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using PInvoke;
using Newtonsoft.Json;
using System.ComponentModel;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<(IntPtr, RECT, IntPtr)> taskbarDetails = new List<(IntPtr, RECT, IntPtr)>();
        public List<KeyValuePair<double, Rectangle>> monitors = new List<KeyValuePair<double, Rectangle>>();
        public Dictionary<IntPtr, double> taskbarScalechart = new Dictionary<IntPtr, double>();
        public bool shouldReallyDieNoReally = false;
        public string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public Settings activeSettings = new Settings();
        public BackgroundWorker bw = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            bw.DoWork += Bw_DoWork;
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            FileSystem();
            activeSettings = ReadJSON();

            marginInput.Text = activeSettings.margin.ToString();
            cornerRadiusInput.Text = activeSettings.cornerRadius.ToString();

            IntPtr hwndMain = FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            GetWindowRect(hwndMain, out RECT rectMain);
            GetWindowRgn(hwndMain, out IntPtr hrgnMain);
            taskbarDetails.Add((hwndMain, rectMain, hrgnMain));

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
                    taskbarDetails.Add((hwndCurrent, rectCurrent, hrgnCurrent));
                }
            }
            if (marginInput.Text != null && cornerRadiusInput.Text != null)
            {
                ApplyButton_Click(null, null);
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
                        GetWindowRect(taskbarDetails[a].Item1, out RECT rectCheck);
                        if (rectCheck.Left != taskbarDetails[a].Item2.Left || rectCheck.Top != taskbarDetails[a].Item2.Top || rectCheck.Right != taskbarDetails[a].Item2.Right || rectCheck.Bottom != taskbarDetails[a].Item2.Bottom)
                        {
                            ResetTaskbar(taskbarDetails[a]);
                            taskbarDetails[a] = (taskbarDetails[a].Item1, rectCheck, taskbarDetails[a].Item3);

                            taskbarScalechart.TryGetValue(taskbarDetails[a].Item1, out double scaleFactor);
                            UpdateTaskbar(taskbarDetails[a], (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item2, scaleFactor, rectCheck);
                        }
                    }
                    System.Threading.Thread.Sleep(50);
                }

            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            taskbarScalechart.Clear();
            int roundFactor = Convert.ToInt32(cornerRadiusInput.Text);
            int marginFactor = Convert.ToInt32(marginInput.Text);
            activeSettings.cornerRadius = roundFactor;
            activeSettings.margin = marginFactor;

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

        public static void ResetTaskbar((IntPtr, RECT, IntPtr) tbDeets)
        {
            SetWindowRgn(tbDeets.Item1, tbDeets.Item3, true);
        }

        public static void UpdateTaskbar((IntPtr, RECT, IntPtr) tbDeets, int marginFactor, int roundFactor, double scaleFactor, RECT rectNew)
        {
            SetWindowRgn(tbDeets.Item1, CreateRoundRectRgn(Convert.ToInt32(marginFactor * scaleFactor), Convert.ToInt32(marginFactor * scaleFactor), Convert.ToInt32((rectNew.Right - rectNew.Left) * scaleFactor) - marginFactor, Convert.ToInt32((rectNew.Bottom - rectNew.Top) * scaleFactor) - marginFactor, Convert.ToInt32(roundFactor * scaleFactor), Convert.ToInt32(roundFactor * scaleFactor)), true);
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



        public class Settings
        {
            public int cornerRadius { get; set; }
            public int margin { get; set; }
        }

        public Settings ReadJSON()
        {
            string jsonSettings = File.ReadAllText(Path.Combine(localFolder, "rtb.json"));
            Settings settings = JsonConvert.DeserializeObject<Settings>(jsonSettings);
            return settings;
        }

        private void WriteJSON()
        {
            File.Create(Path.Combine(localFolder, "rtb.json")).Close();
            File.WriteAllText(Path.Combine(localFolder, "rtb.json"), JsonConvert.SerializeObject(activeSettings, Formatting.Indented));
        }

        private void FileSystem()
        {

            if (!File.Exists(Path.Combine(localFolder, "rtb.json")))
            {
                WriteJSON(); // butts - Missy Quarry, 2020
            }
            if (File.ReadAllText(Path.Combine(localFolder, "rtb.json")) == "" || File.ReadAllText(Path.Combine(localFolder, "rtb.json")) == null)
            {
                WriteJSON(); // Initialises empty file
            }

        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

        enum MonitorOptions : uint
        {
            MONITOR_DEFAULTTONULL = 0x00000000,
            MONITOR_DEFAULTTOPRIMARY = 0x00000001,
            MONITOR_DEFAULTTONEAREST = 0x00000002
        }

        [DllImport("shcore.dll")]
        static extern void GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out int dpiX, out int dpiY);

        private enum MONITOR_DPI_TYPE
        {
            MDT_EFFECTIVE_DPI,
            MDT_ANGULAR_DPI,
            MDT_RAW_DPI,
            MDT_DEFAULT
        };

        [DllImport("user32.dll")]
        static extern int GetWindowRgnBox(IntPtr hWnd, out RECT lprc);

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

    }
}
