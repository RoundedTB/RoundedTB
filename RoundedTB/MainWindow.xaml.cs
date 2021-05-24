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
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using PInvoke;
using System.ComponentModel;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<(IntPtr, RECT, IntPtr)> taskbarDetails = new List<(IntPtr, RECT, IntPtr)>();
        public List<KeyValuePair<double, System.Drawing.Rectangle>> monitors = new List<KeyValuePair<double, System.Drawing.Rectangle>>();
        public Dictionary<IntPtr, double> taskbarScalechart = new Dictionary<IntPtr, double>();

        public BackgroundWorker bw = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            bw.DoWork += Bw_DoWork;
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            IntPtr hwndMain = FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            RECT rectMain;
            GetWindowRect(hwndMain, out rectMain);
            IntPtr hrgnMain;
            GetWindowRgn(hwndMain, out hrgnMain);
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
                    RECT rectCurrent;
                    GetWindowRect(hwndCurrent, out rectCurrent);
                    IntPtr hrgnCurrent;
                    GetWindowRgn(hwndCurrent, out hrgnCurrent);
                    taskbarDetails.Add((hwndCurrent, rectCurrent, hrgnCurrent));
                }
            }

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            foreach (var tbDeets in taskbarDetails)
            {
                ResetTaskbar(tbDeets);
            }
            base.OnClosing(e);
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
                    RECT rectCheck;
                    for (int a = 0; a < taskbarDetails.Count; a++)
                    {
                        GetWindowRect(taskbarDetails[a].Item1, out rectCheck);
                        if (rectCheck.Left != taskbarDetails[a].Item2.Left || rectCheck.Top != taskbarDetails[a].Item2.Top || rectCheck.Right != taskbarDetails[a].Item2.Right || rectCheck.Bottom != taskbarDetails[a].Item2.Bottom)
                        {
                            ResetTaskbar(taskbarDetails[a]);
                            taskbarDetails[a] = (taskbarDetails[a].Item1, rectCheck, taskbarDetails[a].Item3);

                            double scaleFactor;
                            taskbarScalechart.TryGetValue(taskbarDetails[a].Item1, out scaleFactor);
                            UpdateTaskbar(taskbarDetails[a], (((int, int))e.Argument).Item1, (((int, int))e.Argument).Item2, scaleFactor, rectCheck);
                        }
                    }
                    System.Threading.Thread.Sleep(50);
                }

            }
        }

        private void applyButton_Click(object sender, RoutedEventArgs e)
        {
            taskbarScalechart.Clear();
            int roundFactor = Convert.ToInt32(cornerRadiusInput.Text);
            int marginFactor = Convert.ToInt32(marginInput.Text);
            double scaleFactor = 1;

            List<KeyValuePair<double, System.Drawing.Rectangle>> monitors = GetDisplayDetails();

            foreach (var tbDeets in taskbarDetails)
            {
                foreach (KeyValuePair<double, System.Drawing.Rectangle> monitor in monitors)
                {
                    if (monitor.Value.Contains(new System.Drawing.Point(tbDeets.Item2.Left + 2, tbDeets.Item2.Top + 2)))
                    {
                        taskbarScalechart.Add(tbDeets.Item1, monitor.Key);
                        break;
                    }
                }
                UpdateTaskbar(tbDeets, marginFactor, roundFactor, scaleFactor, tbDeets.Item2);
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

        public static void ResetTaskbar((IntPtr, RECT, IntPtr) tbDeets)
        {
            SetWindowRgn(tbDeets.Item1, tbDeets.Item3, true);
        }

        public static void UpdateTaskbar((IntPtr, RECT, IntPtr) tbDeets, int marginFactor, int roundFactor, double scaleFactor, RECT rectNew)
        {
            SetWindowRgn(tbDeets.Item1, CreateRoundRectRgn(Convert.ToInt32(marginFactor * scaleFactor), Convert.ToInt32(marginFactor * scaleFactor), Convert.ToInt32((rectNew.Right - rectNew.Left) * scaleFactor) - marginFactor, Convert.ToInt32((rectNew.Bottom - rectNew.Top) * scaleFactor) - marginFactor, Convert.ToInt32(roundFactor * scaleFactor), Convert.ToInt32(roundFactor * scaleFactor)), true);
        }

        public static List<KeyValuePair<double, System.Drawing.Rectangle>> GetDisplayDetails()
        {
            List<KeyValuePair<double, System.Drawing.Rectangle>> monitors = new List<KeyValuePair<double, System.Drawing.Rectangle>>();
            foreach (Screen screen in Screen.AllScreens)
            {
                DEVMODE dm = new DEVMODE();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                EnumDisplaySettings(screen.DeviceName, -1, ref dm);

                Console.WriteLine($"Device: {screen.DeviceName}");
                Console.WriteLine($"Real Resolution: {dm.dmPelsWidth}x{dm.dmPelsHeight}");
                Console.WriteLine($"Virtual Resolution: {screen.Bounds.Width}x{screen.Bounds.Height}");
                Console.WriteLine($"Position: {dm.dmPositionX}, {dm.dmPositionY}");
                Console.WriteLine();

                double sf = dm.dmPelsWidth / screen.Bounds.Width;
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(dm.dmPositionX, dm.dmPositionY, dm.dmPelsWidth, dm.dmPelsHeight);
                monitors.Add(new KeyValuePair<double, System.Drawing.Rectangle>(sf, rect));
            }
            return monitors;
        }





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
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

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

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }
    }
}
