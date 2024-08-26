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
        public static readonly NotifyIcon NotifyIconG = new NotifyIcon();

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
