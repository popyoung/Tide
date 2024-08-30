using NAudio.Wave;

namespace 潮汐2
{
    public partial class MainWindow
    {
        public class WaveOutEventManager
        {
            public bool AutoRepeat { get; set; } = false;
            private readonly WaveOutEvent outputDevice = new WaveOutEvent();
            private AudioFileReader? initialedReader;
            public WaveOutEventManager()
            {
                outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
            }

            private void OutputDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
            {
#pragma warning disable CS8604 // 忽略 CS8604 警告
                if (AutoRepeat)
                    PlayAudio(initialedReader);
#pragma warning restore CS8604 // 恢复 CS8604 警告
            }

            public void PlayAudio(AudioFileReader audioFileReader, bool autoRepeat = false)
            {
                AutoRepeat = autoRepeat;
                initialedReader = audioFileReader;
                outputDevice.Stop();
                isPaused = false;
                outputDevice.Init(audioFileReader);
                audioFileReader.Position = 0;
                outputDevice.Play();
            }

            private bool isPaused;
            public void PausePlay()
            {
                if (initialedReader != null && initialedReader.Position != initialedReader.Length)
                {
                    if (isPaused)
                    {
                        isPaused = false;
                        outputDevice.Play();
                    }
                    else
                    {
                        isPaused = true;
                        outputDevice.Pause();
                    }
                }
            }

            ~WaveOutEventManager()
            {
                outputDevice.Dispose();
                initialedReader?.Dispose();
            }
        }


    }

}
