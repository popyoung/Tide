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
using System.Windows.Threading;
using HandyControl.Controls;

namespace 潮汐2
{
    /// <summary>
    /// Timer.xaml 的交互逻辑
    /// </summary>
    public partial class TimerWindow : System.Windows.Window
    {
        public readonly new MainWindow Owner;
        private readonly Storyboard storyboardFadeOut = new(), storyboardFadeIn = new();
        private readonly DoubleAnimation animationFadeOut, animationFadeIn;
        public TimerWindow(MainWindow owner)
        {
            InitializeComponent();
            Owner = owner;
            animationFadeOut = new()
            {
                From = Opacity,
                //BeginTime = TimeSpan.FromSeconds(0.5);
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                FillBehavior = FillBehavior.Stop,

            };
            Storyboard.SetTarget(animationFadeOut, this);
            Storyboard.SetTargetProperty(animationFadeOut, new PropertyPath(OpacityProperty));
            storyboardFadeOut.Children.Add(animationFadeOut);
            storyboardFadeOut.Completed += Storyboard_Completed;

            animationFadeIn = new()
            {
                From = 0,
                //BeginTime = TimeSpan.FromSeconds(3),
                To = Opacity,
                Duration = TimeSpan.FromSeconds(1.5),
                FillBehavior = FillBehavior.Stop
            };
            Storyboard.SetTarget(animationFadeIn, this);
            Storyboard.SetTargetProperty(animationFadeIn, new PropertyPath(OpacityProperty));
            storyboardFadeIn.Children.Add(animationFadeIn);

        }

        private void Storyboard_Completed(object? sender, EventArgs e)
        {
            Hide();
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

        public Action<object, MouseButtonEventArgs>? LabelMouseLeftButtonDown;
        private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            LabelMouseLeftButtonDown?.Invoke(sender, e);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        public Action<object, MouseButtonEventArgs>? WindowMouseDoubleClick;
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            animationFadeOut.From = Opacity;
            animationFadeIn.To = Opacity;
            IsEnabled = false;
            Task.Delay(3000).ContinueWith(t =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Show();
                    IsEnabled = true;
                    storyboardFadeIn.Begin();
                }));
            });
            // 播放故事板
            storyboardFadeOut.Begin(this);
        }
    }
}
