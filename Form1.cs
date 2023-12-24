using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Speech
{
    public partial class Form1 : Form
    {
        private string outputFolder = null;
        private string questionFilePath = null;
        private string answerFilePath = null;
        private string startFilePath = null;
        private string endFilePath = null;
        private SpeechPlayback start;
        private SpeechPlayback end;
        private SpeechRecorder recorder;
        private SpeechPlayback playback;
        private SpeechRecognizer recognizer;
        private Chat chat;
        private SpeechSynthesizer synthesizer;
        public Form1()
        {
            InitializeComponent();
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            // Creation folder for audio files
            outputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpeechOutput");
            Directory.CreateDirectory(outputFolder);
            questionFilePath = Path.Combine(outputFolder, "question.wav");
            answerFilePath = Path.Combine(outputFolder, "answer.mp3");
            startFilePath = Path.Combine(outputFolder, "start.mp3");
            endFilePath = Path.Combine(outputFolder, "end.mp3");

            start = new SpeechPlayback(startFilePath);
            end = new SpeechPlayback(endFilePath);
            recorder = new SpeechRecorder(questionFilePath);
            playback = new SpeechPlayback(answerFilePath);
            recognizer = new SpeechRecognizer();
            chat = new Chat();
            synthesizer = new SpeechSynthesizer();

            try
            {
                await recognizer.Init();
                await synthesizer.Init();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            // Event subscriptions
            recorder.SpeechRecordingStopped += async (s, a) => {
                try
                {
                    start.Start();
                    await recognizer.recognizeText(questionFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startButton.Enabled = true;
                }
            };
            playback.EventPlaybackStopped += (s, a) =>
            {
                try
                {
                    end.Start();
                    startButton.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startButton.Enabled = true;
                }
            };
            recognizer.SpeechRecognitionCompleted += async (s, a) => {
                try
                {
                    questionLabel.Text = recognizer.RecognizedText;
                    await chat.chatRequest(recognizer.RecognizedText);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startButton.Enabled = true;
                }
            };
            chat.AnswerCompleted += async (s, a) => {
                try
                {
                    answerTextBox.Text = chat.Answer;
                    await synthesizer.speechSynthesis(chat.Answer, answerFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startButton.Enabled = true;
                }  
            };
            synthesizer.SpeechSynthesisCompleted += async (s, a) => {
                try
                {
                    playback.Start();
                    await listenStopCommand();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    startButton.Enabled = true;
                }
            };
        }
        // Start recording 
        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                recorder.Start();
                startButton.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            recorder?.Stop();
            playback?.Stop();
        }
        // Voice recording during answer playback (every 1300 milliseconds) and sending for recognition
        // If one of the stop commands was said, playback of the answer stops
        private async Task listenStopCommand()
        {
            try
            {
                int i = 0;
                SpeechRecognizer listener = new SpeechRecognizer();
                await listener.Init();
                TaskCompletionSource<bool> recognitionCompleted = new TaskCompletionSource<bool>();
                listener.SpeechRecognitionCompleted += (s, a) => {
                    if (listener.RecognizedText.IndexOf("стоп", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        playback.Stop();
                    }
                    else if (listener.RecognizedText.IndexOf("хватит", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        playback.Stop();
                    }
                    recognitionCompleted.SetResult(true);
                };
                while (playback.PlaybackInProgress)
                {
                    string listenerFilePath = Path.Combine(outputFolder, $"listener{i++}.wav");
                    SpeechRecorder recSecond = new SpeechRecorder(listenerFilePath);
                    TaskCompletionSource<bool> recordingStopped = new TaskCompletionSource<bool>();
                    recSecond.SpeechRecordingStopped += (s, e) => recordingStopped.SetResult(true);
                    recSecond.Start();
                    await Task.Delay(1300);
                    recSecond.Stop();
                    await recordingStopped.Task;
                    recognitionCompleted = new TaskCompletionSource<bool>();
                    listener.recognizeText(listenerFilePath);
                }
                await recognitionCompleted.Task;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
