using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using HugeHard.Json;
using Newtonsoft.Json;

namespace 潮汐
{
    public partial class Form1 : Form
    {
        class Config : JsonConfig<Form1>
        {
            public int 背景音乐
            {
                get { return Host.comboBox1.SelectedIndex; }
                set { Host.comboBox1.SelectedIndex = Math.Min(value, Host.comboBox1.Items.Count - 1); }
            }

            public decimal 工作时间
            {
                get { return Host.numericUpDown1.Value; }
                set { Host.numericUpDown1.Value = value; }
            }
            public decimal 休息时间
            {
                get { return Host.numericUpDown2.Value; }
                set { Host.numericUpDown2.Value = value; }
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        public Form1()
        {
            InitializeComponent();
            music_player.MediaEnded += Player_MediaEnded;
            comboBox1.SelectedIndex = 0;
        }

        private void Player_MediaEnded(object sender, EventArgs e)
        {
            music_player.Position = new TimeSpan();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jsonHelper.Save("config.json", config);
            Application.Exit();
        }

        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            //Show();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                switch (state)
                {
                    case States.未启动:
                        StartWork();
                        backgroundWorker1.RunWorkerAsync();
                        break;
                    case States.工作:
                    case States.休息:
                        state = States.未启动;
                        StopMusic();
                        break;
                }
            }
        }

        List<string> music = new List<string>()
        {
            null,
            "./resources/meditation.wma",
            "./resources/ocean.wma",
            "./resources/rain.wma",
            "./resources/forest.wma",
            "./resources/coffee.wma",
        };

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                switch (state)
                {
                    case States.工作:
                        if (countDown.TotalSeconds <= 0)
                            StartBreak();
                        break;
                    case States.休息:
                        if (countDown.TotalSeconds <= 0)
                            StartWork();
                        break;
                }
            }
        }

        private void StartWork()
        {
            countDown = new TimeSpan(0, (int)config.工作时间, 0);
            state = States.工作;
            PlayEffect(1);
            PlayMusic(music[config.背景音乐]);
        }

        private void StartBreak()
        {
            countDown = new TimeSpan(0, (int)config.休息时间, 0);
            state = States.休息;
            PlayEffect(1);
        }

        enum States
        {
            未启动,
            工作,
            休息,
        }

        States state;
        TimeSpan countDown;
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            countDown = countDown.Subtract(new TimeSpan(0, 0, 1));
            Thread.Sleep(1000);
        }

        int gc_count = 0;

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (gc_count++ % 100 == 0)
                {
                    gc_count = 1;
                    GC.Collect();
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
            ShowTip();
            if (state == States.未启动)
                return;
            if (countDown.TotalSeconds == 0)
                StopMusic();
            if (countDown.TotalSeconds <= 0 && countDown.TotalSeconds % 30 == 0)
            {
                //switch (state)
                //{
                //    case States.工作:
                //        StartBreak();
                //        break;
                //    case States.休息:
                //        StartWork();
                //        break;
                //}
                PlayEffect(0);
            }
            //else
            backgroundWorker1.RunWorkerAsync();
        }

        private void ShowTip()
        {
            StringBuilder s = new StringBuilder();
            s.Append("潮汐: ");
            s.AppendLine(state.ToString());
            switch (state)
            {
                case States.未启动:
                    s.AppendLine("双击开始工作!");
                    break;
                case States.工作:
                    if (countDown.TotalSeconds <= 0)
                        s.AppendLine("单击开始休息!");
                    else
                        s.AppendLine($"剩余时间: {countDown}");
                    break;
                case States.休息:
                    if (countDown.TotalSeconds <= 0)
                        s.AppendLine("单击开始工作!");
                    else
                        s.AppendLine($"剩余时间: {countDown}");
                    break;
            }
            notifyIcon1.Text = s.ToString();
        }

        JsonHelper<Config, Form1> jsonHelper;
        Config config;
        private void Form1_Shown(object sender, EventArgs e)
        {
            jsonHelper = new JsonHelper<Config, Form1>(this);
            config = jsonHelper.Load("config.json");
            ShowTip();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                //Hide();
                ShowInTaskbar = true;
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
            }
        }

        MediaPlayer music_player = new MediaPlayer();
        SoundPlayer player = new SoundPlayer();
        string[] soundEffects = { "./resources/清脆提示音.wav", "./resources/Windows Notify.wav" };
        string lastMusic;
        private void PlayMusic(string fileName)
        {
            if (fileName == null)
                return;
            if (fileName != lastMusic)
            {
                music_player.Close();
                music_player.Open(new Uri(fileName, UriKind.Relative));
                lastMusic = fileName;
            }
            music_player.Play();
        }

        private void StopMusic()
        {
            music_player.Stop();
        }

        private void PlayEffect(int index)
        {
            player.SoundLocation = soundEffects[index];
            player.Play();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
