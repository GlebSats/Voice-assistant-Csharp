using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Speech
{
    public class SpeechRecorder
    {
        private WaveInEvent waveSource = null;
        private WaveFileWriter writer = null;
        private string outputFilePath = null;
        public event EventHandler SpeechRecordingStopped;
        public SpeechRecorder(string outputFilePath) 
        {
            this.outputFilePath = outputFilePath;
        } 
        public void Start()
        {
            try
            {
                DateTime startOfSilence = DateTime.MaxValue;
                waveSource = new WaveInEvent();
                writer = new WaveFileWriter(outputFilePath, waveSource.WaveFormat);
                // Start recording when data is available (max 20 seconds) and stop if there is silence for a 1 second
                waveSource.DataAvailable += (s, a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                    if (IsSilence(a.Buffer, a.BytesRecorded))
                    {
                        if (startOfSilence == DateTime.MaxValue)
                        {
                            startOfSilence = DateTime.Now;
                        }
                    }
                    else
                    {
                        startOfSilence = DateTime.MaxValue;
                    }
                    if (writer.Position > waveSource.WaveFormat.AverageBytesPerSecond * 20)
                    {
                        waveSource.StopRecording();
                    }
                    if ((DateTime.Now - startOfSilence).TotalMilliseconds > 1000)
                    {
                        waveSource.StopRecording();
                    }
                };
                waveSource.RecordingStopped += (s, a) =>
                {
                    writer?.Dispose();
                    writer = null;
                    waveSource?.Dispose();
                    waveSource = null;
                    SpeechRecordingStopped?.Invoke(this, null);
                };
                waveSource.StartRecording();
            }
            catch (Exception ex)
            {
                throw new Exception($"Speech recording error: {ex.Message}");
            }
        }
        // Noise level check
        private bool IsSilence(byte[] buffer, int bytesRecorded)
        {
            for (int i = 0; i < bytesRecorded; i+=2) 
            {
                short sample = BitConverter.ToInt16(buffer, i);
                if (Math.Abs(sample) > 500)
                {
                    return false;
                } 
            }
            return true;
        }
        public void Stop()
        {
            waveSource?.StopRecording();
        }

    }
}
