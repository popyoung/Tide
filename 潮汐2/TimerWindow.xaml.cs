using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HandyControl.Controls;

namespace 潮汐2
{
    /// <summary>
    /// Timer.xaml 的交互逻辑
    /// </summary>
    public partial class TimerWindow : System.Windows.Window
    {
        //MainWindow Owner;

        public TimerWindow()
        {
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            Hide();
            e.Cancel = true;
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.OriginalSource is not Border && e.OriginalSource is not TextBlock)
            //    if (e.LeftButton == MouseButtonState.Pressed)
            //        DragMove();
        }

        public Action<object, MouseButtonEventArgs>? LabelMouseLeftButtonDown;
        private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LabelMouseLeftButtonDown?.Invoke(sender, e);
        }

        private void ProgressBar2_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();

        }
    }
}
