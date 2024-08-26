using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using HandyControl.Controls;
using HandyControl.Interactivity;
using HandyControl.Tools.Extension;
using NAudio.Wave;
using Window = System.Windows.Window;

namespace 潮汐2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum States { 工作中, 休息中, 待工作, 待休息 }
        private readonly DispatcherTimer timer = new();
        private int remainingSeconds;
        private States state = States.待工作;
        public States State
        {
            get => state; set
            {

                state = value;
            }
        }
        public class MyDataModel : INotifyPropertyChanged
        {
            //public int WorkTime { get; set; } = 25;
            private int workTime;

            public int WorkTime
            {
                get { return workTime; }
                set
                {
                    workTime = value;
                    OnPropertyChanged(nameof(workTime));
                }
            }
            private int restTime;

            public int RestTime
            {
                get { return restTime; }
                set
                {
                    restTime = value;
                    OnPropertyChanged(nameof(restTime));
                }
            }
            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private readonly MyDataModel model = new(); //MyDataModel
        private readonly List<MusicItem> musicList = [];

        public TimerWindow timerWindow;
        private readonly WaveOutEvent outputDevice = new WaveOutEvent();
        private readonly AudioFileReader audioNotifyStart = new AudioFileReader(Environment.CurrentDirectory + "/Resources/Windows Notify.wav");
        private readonly AudioFileReader audioNotifyFinish = new AudioFileReader(Environment.CurrentDirectory + "/Resources/清脆提示音.wav");
        private AudioFileReader? audioMusic;
        public MainWindow()
        {
            InitializeComponent();

            timerWindow = new TimerWindow();

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            App.NotifyIconG.Icon = Icon;
            App.NotifyIconG.ShowBalloonTip("潮汐2 已启动", "", HandyControl.Data.NotifyIconInfoType.Info);
            //App.NotifyIconG.MouseDoubleClick += NotifyIconG_MouseDoubleClick;
            App.NotifyIconG.Click += StartButton_Click;

            ContextMenu menu = new()
            {
                FontFamily = FontFamily,
                FontSize = FontSize * 0.8
            };
            MenuItem newItem = new MenuItem() { Header = "启动" };
            newItem.Click += (s, e) => StartButton_Click(s, e);
            menu.Items.Add(newItem);

            newItem = new MenuItem() { Header = "跳过", };
            newItem.Click += (s, e) => SkipButton_Click(s, e);
            menu.Items.Add(newItem);

            newItem = new MenuItem() { Header = "重置", };
            newItem.Click += (s, e) => ResetButton_Click(s, e);
            menu.Items.Add(newItem);

            newItem = new MenuItem() { Header = "显示浮窗", };
            newItem.Click += (s, e) => timerWindow.Show();
            menu.Items.Add(newItem);

            menu.Items.Add(new MenuItem() { Header = "设置", Command = ControlCommands.PushMainWindow2Top });
            menu.Items.Add(new MenuItem() { Header = "退出", Command = ControlCommands.ShutdownApp });
            App.NotifyIconG.ContextMenu = menu;
            timerWindow.ContextMenu = menu;
            Hide();

            musicList.Add(new MusicItem("无", ""));
            musicList.Add(new MusicItem("冥想曲", "meditation.m4a"));
            musicList.Add(new MusicItem("咖啡店", "coffee.m4a"));
            musicList.Add(new MusicItem("森林", "forest.m4a"));
            musicList.Add(new MusicItem("海洋", "ocean.m4a"));
            musicList.Add(new MusicItem("雨天", "rain.m4a"));
            musicComboBox.ItemsSource = musicList;
            musicComboBox.SelectedIndex = 0;

            DataContext = model;

            Binding b = new Binding
            {
                Path = new PropertyPath(OpacityProperty),
                Source = timerWindow,
            };
            PreviewSliderHorizontal.SetBinding(PreviewSlider.ValueProperty, b);

            LoadConfigFromJson();
            var temp = timerWindow.Left;
            timerWindow.Left = 0;
            timerWindow.Show();

            // 创建故事板
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animationMove = new DoubleAnimation();
            animationMove.From = 0;
            //animationMove.BeginTime = TimeSpan.FromSeconds(0.5);
            animationMove.To = temp;
            animationMove.Duration = TimeSpan.FromSeconds(1.5);
            animationMove.DecelerationRatio = 1;
            animationMove.FillBehavior = FillBehavior.Stop;
            Storyboard.SetTarget(animationMove, timerWindow);
            Storyboard.SetTargetProperty(animationMove, new PropertyPath("Left"));
            storyboard.Children.Add(animationMove);
            // 播放故事板
            storyboard.Begin(this);
            storyboard.Completed += (s, e) => timerWindow.Left = temp;
        }


        ~MainWindow()
        {
            outputDevice.Dispose();
            audioNotifyStart.Dispose();
            audioNotifyFinish.Dispose();
            audioMusic?.Dispose();
        }


        private void NotifyIconG_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            remainingSeconds--;
            if (remainingSeconds <= 0)
            {
                timer.Stop();
                outputDevice.Stop();
                outputDevice.Init(audioNotifyFinish);
                audioNotifyFinish.Position = 0;
                outputDevice.Play();
                State = State switch
                {
                    States.工作中 => States.待休息,
                    States.休息中 => States.待工作,
                    _ => throw new InvalidOperationException("Invalid state! Should never happen."),
                };
            }
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            if (timer.IsEnabled)
            {
                timerWindow.progressBar.Value = 100 - remainingSeconds * 100.0 / totalSeconds;
                TimeSpan timeSpan = TimeSpan.FromSeconds(remainingSeconds);
                if (timeSpan.Hours < 1)
                {
                    timerWindow.label.Text = timeSpan.ToString(@"mm\:ss");
                    Canvas.SetLeft(timerWindow.label, 35);
                }
                else
                {
                    timerWindow.label.Text = timeSpan.ToString();
                    Canvas.SetLeft(timerWindow.label, 23);
                }
            }
            else
            {
                timerWindow.label.Text = State.ToString();
                Canvas.SetLeft(timerWindow.label, 30);
                timerWindow.progressBar.Value = 100;
            }
        }

        private int totalSeconds = 0;
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            switch (State)
            {
                case States.待工作:
                    State = States.工作中;
                    outputDevice.Stop();
                    outputDevice.Init(audioNotifyStart);
                    audioNotifyStart.Position = 0;
                    outputDevice.Play();
                    break;
                case States.待休息:
                    State = States.休息中;
                    if (musicComboBox.SelectedIndex != 0 && audioMusic != null)
                    {
                        outputDevice.Stop();
                        audioMusic.Position = 0;
                        outputDevice.Init(audioMusic);
                        outputDevice.Play();
                    }
                    break;
                default:
                    return;
            }
            remainingSeconds = State == States.休息中 ? model.RestTime * 60 : model.WorkTime * 60;
            totalSeconds = remainingSeconds;

            timer.Start();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            if (State == States.工作中 || State == States.休息中)
                remainingSeconds = 1;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (State == States.工作中 || State == States.休息中)
            {
                State = States.休息中;
                remainingSeconds = 1;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        public class MusicItem(string title, string path)
        {
            public string Title { get; set; } = title;
            public string Path { get; set; } = Environment.CurrentDirectory + "/Resources/" + path;
        }

        private void MusicComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (musicComboBox.SelectedIndex != 0)
            {
                if (State == States.休息中)
                    outputDevice.Stop();
                audioMusic?.Dispose();
                audioMusic = new AudioFileReader(musicComboBox.SelectedValue.ToString());
                if (State == States.休息中)
                {
                    outputDevice.Init(audioMusic);
                    outputDevice.Play();
                }
            }
        }

        public class AppConfig(int restTime, int workTime, int musicComboBoxIndex, double opacity, double top, double left)
        {
            public int RestTime { get; set; } = restTime;
            public int WorkTime { get; set; } = workTime;
            public int MusicComboBoxIndex { get; set; } = musicComboBoxIndex;
            public double Opacity { get; set; } = opacity;
            public double Top { get; set; } = top;
            public double Left { get; set; } = left;

            public (int, int, int, double, double, double) ToTupleValue()
            {
                return (RestTime, WorkTime, MusicComboBoxIndex, Opacity, Top, Left);
            }
        }

        private void SaveConfigToJson()
        {
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            AppConfig appConfig = new AppConfig(model.RestTime, model.WorkTime, musicComboBox.SelectedIndex, timerWindow.Opacity, timerWindow.Top, timerWindow.Left);
            string json = JsonSerializer.Serialize(appConfig, options);
            File.WriteAllText("config.json", json);
        }

        private void LoadConfigFromJson()
        {
            if (File.Exists("config.json"))
            {
                string json = File.ReadAllText("config.json");
                var data = JsonSerializer.Deserialize<AppConfig>(json);
                if (data != null)
                    (model.RestTime, model.WorkTime, musicComboBox.SelectedIndex, timerWindow.Opacity, timerWindow.Top, timerWindow.Left) = data.ToTupleValue();
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveConfigToJson();
        }

    }

}
