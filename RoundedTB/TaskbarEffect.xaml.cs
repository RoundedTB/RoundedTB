using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for TaskbarEffect.xaml
    /// </summary>
    public partial class TaskbarEffect : Window
    {
        BackgroundWorker backgroundWorker = new BackgroundWorker();
        public POINT pOINT = new POINT();
        public Point pp = new Point();

        public TaskbarEffect()
        {

            InitializeComponent();
            { // Code for removing from Alt+Tab
                Window w = new Window();
                w.Top = -10000;
                w.Left = -10000;
                w.Width = 1;
                w.Height = 1;
                w.WindowStyle = WindowStyle.ToolWindow;
                w.Show();
                Owner = w;
                w.Hide();
            }
            Show();

            mwin.Top = 0;
            mwin.Left = 0;
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerAsync();
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => MoveTheThingy()));
                Thread.Sleep(10);
            }
        }
        public void MoveTheThingy()
        {
            GetCursorPos(out pOINT);
            Point pp = mwin.PointFromScreen(pOINT);
            Canvas.SetLeft(eye, pp.X - (eye.Width / 2));
            Canvas.SetTop(eye, pp.Y - (eye.Height / 2));

        }




        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);


        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }
    }
}
