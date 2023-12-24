using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Speech
{
    // Audio file playback
    public class SpeechPlayback
    {
        private WaveOutEvent outputSource = null;
        private AudioFileReader audioFile = null;
        private string inputFilePath = null;
        public bool PlaybackInProgress { get; private set; }
        public event EventHandler EventPlaybackStopped;
        public SpeechPlayback(string inputFilePath)
        {
            this.inputFilePath = inputFilePath;
            PlaybackInProgress = false;
        }
        public void Start()
        {
            try
            {
                if (outputSource == null)
                {
                    outputSource = new WaveOutEvent();
                    outputSource.PlaybackStopped += (s, a) =>
                    {
                        outputSource?.Dispose();
                        outputSource = null;
                        audioFile?.Dispose();
                        audioFile = null;
                        PlaybackInProgress = false;
                        EventPlaybackStopped?.Invoke(this, null);
                    };
                }
                if (audioFile == null)
                {
                    audioFile = new AudioFileReader(inputFilePath);
                    outputSource.Init(audioFile);
                }
                outputSource.Play();
                PlaybackInProgress = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Speech playback error: {ex.Message}");
            } 
        }
        public void Stop()
        {
            outputSource?.Stop();
        }
    }
}
