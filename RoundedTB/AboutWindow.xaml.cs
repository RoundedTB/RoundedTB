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
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Diagnostics;

namespace RoundedTB
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            WPFUI.Background.Manager.Apply(WPFUI.Background.BackgroundType.Mica, this);
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void configButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((MainWindow)Application.Current.MainWindow).configPath);
        }

        private void logButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(((MainWindow)Application.Current.MainWindow).logPath);
        }
    }
}
