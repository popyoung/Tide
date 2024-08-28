using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
using static 潮汐2.MainWindow;
using Window = System.Windows.Window;

namespace 潮汐2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum States { 工作中, 休息中, 待工作, 待休息 }

        private int actionThreshold = 180;
        private readonly DispatcherTimer timer = new();
        //private readonly DispatcherTimer timer2 = new();
        //private int timer2Clock = 0;
        private int remainingSeconds;
        private int idleSeconds;
        private int inertia = 15;
        private States state = States.待工作;
        private readonly GlobalInputMonitor monitor = new();
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
            private int workTime = 25;

            public int WorkTime
            {
                get { return workTime; }
                set
                {
                    workTime = value;
                    OnPropertyChanged(nameof(workTime));
                }
            }

            private int restTime = 5;

            public int RestTime
            {
                get { return restTime; }
                set
                {
                    restTime = value;
                    OnPropertyChanged(nameof(restTime));
                }
            }

            private int alertInterval = 180;

            public int AlertInterval
            {
                get { return alertInterval; }
                set
                {
                    alertInterval = value;
                    OnPropertyChanged(nameof(alertInterval));
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
        private readonly WaveOutEventManager waveOutEventManager = new();
        private readonly AudioFileReader audioNotifyStart = new AudioFileReader(Environment.CurrentDirectory + "/Resources/Windows Notify.wav");
        private readonly AudioFileReader audioNotifyFinish = new AudioFileReader(Environment.CurrentDirectory + "/Resources/清脆提示音.wav");
        private AudioFileReader? audioMusic;
        private readonly NotifyIcon notifyIcon;
        public MainWindow()
        {
            InitializeComponent();

            timerWindow = new TimerWindow();

            notifyIcon = ((App)Application.Current).NotifyIconG;
            notifyIcon.Icon = Icon;
            notifyIcon.BlinkInterval = TimeSpan.FromMilliseconds(500);
            notifyIcon.ShowBalloonTip("潮汐2 已启动", "", HandyControl.Data.NotifyIconInfoType.Info);
            //App.NotifyIconG.MouseDoubleClick += NotifyIconG_MouseDoubleClick;
            notifyIcon.Click += StartButton_Click;

            ContextMenu menu = new()
            {
                FontFamily = FontFamily,
                FontSize = FontSize * 0.8
            };
            MenuItem newItem = new MenuItem() { Header = "开始" };
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
            notifyIcon.ContextMenu = menu;
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
            Storyboard storyboard = new();
            DoubleAnimation animationMove = new()
            {
                From = 0,
                //BeginTime = TimeSpan.FromSeconds(0.5);
                To = temp,
                Duration = TimeSpan.FromSeconds(1.5),
                DecelerationRatio = 1,
                FillBehavior = FillBehavior.Stop
            };
            Storyboard.SetTarget(animationMove, timerWindow);
            Storyboard.SetTargetProperty(animationMove, new PropertyPath("Left"));
            storyboard.Children.Add(animationMove);
            // 播放故事板
            storyboard.Begin(this);
            storyboard.Completed += (s, e) => timerWindow.Left = temp;

            monitor.KeyMsgReceived += Monitor_KeyMsgReceived;
            monitor.MouseMsgReceived += Monitor_MouseMsgReceived;
            monitor.MouseMinMovement = 5;

            timer.Interval = TimeSpan.FromMilliseconds(33);
            timer.Tick += Timer_Tick;
            TomatoStart();
            timer.Start();

        }

        private void Monitor_MouseMsgReceived(GlobalInputMonitor.MouseMsgReceivedEventArgs args)
        {
            ActAgain();
            //throw new NotImplementedException();
        }

        private void Monitor_KeyMsgReceived(GlobalInputMonitor.KeyMsgReceivedEventArgs args)
        {
            ActAgain();
        }

        ~MainWindow()
        {
            audioNotifyStart.Dispose();
            audioNotifyFinish.Dispose();
            audioMusic?.Dispose();
            process.Dispose();
        }


        private void NotifyIconG_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Show();
        }

        private int ProgressBarMax => Math.Max(model.RestTime * 60, actionThreshold);
        private DateTime timeStart;

        private int lastTickSecond;
        private bool? lastVideoCheckedResult = null;
        private void ActAgain()
        {
            //if (State == States.工作中)
            //{
            if (idleSeconds > inertia /*&& idleSeconds <= actionThreshold*/)
                if (State != States.工作中 || idleSeconds <= actionThreshold)
                    remainingSeconds -= idleSeconds - inertia;
            idleSeconds = 0;
            lastVideoCheckedResult = null;
            //}
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var t = (int)(DateTime.Now - timeStart).TotalSeconds;
            if (t > lastTickSecond)
                lastTickSecond = t;
            else
                return;

            //测试代码
            //ActAgain();
            //inertia = 3;
            //actionThreshold = 30;

            remainingSeconds--;
            //无操作过了惯性时间后
            if (idleSeconds++ > inertia)
            {
                lastVideoCheckedResult ??= CheckVideoPlaying();
                //无视频播放则停表
                if (lastVideoCheckedResult == false)
                    remainingSeconds++;
                //有视频播放则清空空闲时间
                else
                    idleSeconds = 0;
            }

            //if (idleSeconds++ <= inertia)
            //    remainingSeconds--;
            //else if (lastVideoCheckedResult == null)
            //{
            //    lastVideoCheckedResult = CheckVideoPlaying();
            //    if (lastVideoCheckedResult == true)
            //    {
            //        remainingSeconds--;
            //        idleSeconds = 0;
            //    }
            //}
            //if (lastVideoCheckedResult == true)
            //{
            //    remainingSeconds--;
            //    idleSeconds = 0;
            //}

            //空闲时间超出阈值和休息时间则重新开始番茄钟
            if (idleSeconds >= ProgressBarMax)
                //remainingSeconds = totalSeconds;
                if (State != States.工作中 || remainingSeconds != totalSeconds)
                    TomatoReset();

            if (State == States.工作中 || State == States.休息中)
            {

                if (remainingSeconds <= 0)
                {
                    notifyIcon.IsBlink = true;
                    State = State switch
                    {
                        States.工作中 => States.待休息,
                        States.休息中 => States.待工作,
                        _ => throw new InvalidOperationException("Invalid state! Should never happen."),
                    };
                }
                UpdateTimerText();
            }
            if (State == States.待休息 || State == States.待工作)
            {
                if (remainingSeconds <= 0)
                {
                    waveOutEventManager.PlayAudio(audioNotifyFinish);
                    remainingSeconds = model.AlertInterval;
                }
            }
        }

        private readonly Process process = new Process { StartInfo = processStartInfo };
        private static readonly ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = "powercfg.exe",
            Arguments = "/requests",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,

        };
        private bool CheckVideoPlaying()
        {
            try
            {
                process.Start();
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                //Trace.WriteLine(output.ToString());
                return !IsDisplayNone(output);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.Message);
                return false;
            }
        }

        private void UpdateTimerText()
        {
            if (State == States.工作中 || State == States.休息中)
            {
                timerWindow.progressBar.Value = 100 - remainingSeconds * 100.0 / totalSeconds;
                if (State == States.工作中)
                    timerWindow.progressBar2.Value = idleSeconds * 100.0 / ProgressBarMax;
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
                timerWindow.progressBar2.Value = 0;
            }
        }

        private int totalSeconds = 0;
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            TomatoStart();
        }

        private void TomatoStart()
        {
            switch (State)
            {
                case States.待工作:
                    State = States.工作中;
                    remainingSeconds = model.WorkTime * 60;
                    waveOutEventManager.PlayAudio(audioNotifyStart);
                    break;
                case States.待休息:
                    State = States.休息中;
                    remainingSeconds = model.RestTime * 60;
                    if (musicComboBox.SelectedIndex != 0 && audioMusic != null)
                        waveOutEventManager.PlayAudio(audioMusic, true);
                    break;
                case States.工作中:
                case States.休息中:
                default:
                    return;
            }
            notifyIcon.IsBlink = false;
            totalSeconds = remainingSeconds;
            timeStart = DateTime.Now;
            lastTickSecond = 0;
            //remainingSeconds = State == States.休息中 ? model.RestTime * 60 : model.WorkTime * 60;

            //remainingSeconds = State switch
            //{
            //    States.待休息 => model.AlertInterval,
            //    States.待工作 => model.AlertInterval,
            //    States.工作中 => model.WorkTime * 60,
            //    States.休息中 => model.RestTime * 60,
            //    _ => throw new NotImplementedException()
            //};
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            TomatoSkip();
        }

        private void TomatoSkip()
        {
            State = State switch
            {
                States.工作中 => States.待休息,
                States.休息中 => States.待工作,
                _ => throw new InvalidOperationException("Invalid state! Should never happen."),
            };
            TomatoStart();

            //测试代码
            //remainingSeconds = 1;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            TomatoReset();
        }

        private void TomatoReset()
        {
            State = States.待工作;
            TomatoStart();
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
                audioMusic?.Dispose();
                audioMusic = new AudioFileReader(musicComboBox.SelectedValue.ToString());
                if (State == States.休息中)
                    waveOutEventManager.PlayAudio(audioMusic, true);
            }
        }

        public class AppConfig(int restTime, int workTime, int alertInterval, int musicComboBoxIndex, double opacity, double top, double left)
        {
            public int RestTime { get; set; } = restTime;
            public int WorkTime { get; set; } = workTime;
            public int AlertInterval { get; set; } = alertInterval;
            public int MusicComboBoxIndex { get; set; } = musicComboBoxIndex;
            public double Opacity { get; set; } = opacity;
            public double Top { get; set; } = top;
            public double Left { get; set; } = left;

            public (int, int, int, int, double, double, double) ToTupleValue()
            {
                return (RestTime, WorkTime, AlertInterval, MusicComboBoxIndex, Opacity, Top, Left);
            }
        }

        readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
        private void SaveConfigToJson()
        {
            AppConfig appConfig = new AppConfig(model.RestTime, model.WorkTime, model.AlertInterval, musicComboBox.SelectedIndex, timerWindow.Opacity, timerWindow.Top, timerWindow.Left);
            string json = JsonSerializer.Serialize(appConfig, jsonSerializerOptions);
            File.WriteAllText("config.json", json);
        }

        private void LoadConfigFromJson()
        {
            if (File.Exists("config.json"))
            {
                try
                {
                    string json = File.ReadAllText("config.json");
                    var data = JsonSerializer.Deserialize<AppConfig>(json);
                    if (data != null)
                        (model.RestTime, model.WorkTime, model.AlertInterval, musicComboBox.SelectedIndex, timerWindow.Opacity, timerWindow.Top, timerWindow.Left) = data.ToTupleValue();
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Error(ex.Message, "配置文件读取错误");
                }

            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveConfigToJson();
        }

        [GeneratedRegex(@"DISPLAY:\s*None")]
        private static partial Regex MyRegexDisplayNone();
        private static bool IsDisplayNone(string text)
        {
            return MyRegexDisplayNone().IsMatch(text);
        }

    }

}
