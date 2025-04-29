using System.Configuration;
using System.Data;
using System.Windows;
using HandyControl.Controls;
using HandyControl.Tools.Extension;

namespace 潮汐2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public readonly NotifyIcon NotifyIconG = new();

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(ex.Message, "全局异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            HandyControl.Controls.MessageBox.Show(e.Exception.Message, "主线程异常", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            NotifyIconG.Init();
            //notifyIcon.Show();
            //notifyIcon.Icon = MainWindow.Icon;
            NotifyIconG.Text = "潮汐2";
            //base.OnStartup(e);
        }

    }

}
