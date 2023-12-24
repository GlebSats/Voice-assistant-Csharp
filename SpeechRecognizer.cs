using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Speech
{
    // Speech recognizer using VK API
    public class SpeechRecognizer
    {
        private readonly HttpClient vkClient;
        private string upload_url;
        private string task_id;
        private string responseString;
        private string VK_ACCESS_TOKEN = "YOUR VK ACCESS TOKEN";
        private string version = "5.199";
        private string inputFilePath;
        public string RecognizedText {get; private set;}
        public event EventHandler SpeechRecognitionCompleted;
        public SpeechRecognizer()
        {
            vkClient = new HttpClient();
        }
        public async Task Init()
        {
            // Getting upload url to download the audio file
            try
            {
                HttpResponseMessage response = await vkClient.GetAsync($"https://api.vk.com/method/asr.getUploadUrl?access_token={VK_ACCESS_TOKEN}&v={version}");
                string vkAnswer = await response.Content.ReadAsStringAsync();
                JObject vkJobject = JObject.Parse(vkAnswer);
                var responseToken = vkJobject["response"];
                if (responseToken == null)
                {
                    throw new Exception($"No response token: {vkAnswer}");
                }
                var uploadUrlToken = vkJobject["response"]["upload_url"];
                if (uploadUrlToken == null)
                {
                    throw new Exception($"No upload_url token: {vkAnswer}"); 
                }
                upload_url = uploadUrlToken.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"VK getting upload url error: {ex.Message}");
            } 
        }
        public async Task recognizeText(string inputFilePath)
        {
            try
            {
                this.inputFilePath = inputFilePath;
                await sendAudioFile();
                await sendRequest();
                await getRecognizedText();
                SpeechRecognitionCompleted?.Invoke(this, null);
            }
            catch (Exception)
            {
                throw;
            }
        }
        // Sending audio file 
        private async Task sendAudioFile()
        {
            try
            {
                var content = new MultipartFormDataContent();
                using (var fileStream = new FileStream(inputFilePath, FileMode.Open))
                {
                    content.Add(new StreamContent(fileStream), "file", "recorded.wav");
                    var response = await vkClient.PostAsync(upload_url, content);
                    responseString = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"VK Error sending file for recognition: {ex.Message}");
            }
        }
        // Perform speech recognition from the loaded audio file
        private async Task sendRequest()
        {
            try
            {
                HttpResponseMessage response = await vkClient.GetAsync($"https://api.vk.com/method/asr.process?access_token={VK_ACCESS_TOKEN}&v={version}&audio={responseString}&model=neutral");
                string jsonString = await response.Content.ReadAsStringAsync();
                JObject jobject = JObject.Parse(jsonString);
                var responseToken = jobject["response"];
                if (responseToken == null)
                {
                    throw new Exception($"No response token: {jsonString}");
                }
                var taskIdToken = jobject["response"]["task_id"];
                if (taskIdToken == null)
                {
                    throw new Exception($"No task_id token: {jsonString}");
                }
                task_id = taskIdToken.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"VK Send request error: {ex.Message}");
            }
        }
        // Check the status and getting text transcript of the audio recording
        private async Task getRecognizedText()
        {
            JToken statusToken;
            try
            {
                do
                {
                    HttpResponseMessage response = await vkClient.GetAsync($"https://api.vk.com/method/asr.checkStatus?access_token={VK_ACCESS_TOKEN}&v={version}&task_id={task_id}");
                    string jsonString = await response.Content.ReadAsStringAsync();
                    JObject jobject = JObject.Parse(jsonString);
                    var responseToken = jobject["response"];

                    if (responseToken == null)
                        throw new Exception($"No response token: {jsonString}");

                    statusToken = jobject["response"]["status"];
                    if (statusToken == null)
                        throw new Exception($"No status token: {jsonString}");

                    if (statusToken.ToString() != "finished")
                    {
                        await Task.Delay(500);
                    }
                    else
                    {
                        var textToken = jobject["response"]["text"];
                        if (textToken != null)
                        {
                            RecognizedText = textToken.ToString();
                        }
                        else
                        {
                            throw new Exception($"No text token: {jsonString}");
                        }
                    }
                } while (statusToken.ToString() != "finished");
            }
            catch (Exception ex)
            {
                throw new Exception($"VK getting recognized text error: {ex.Message}");
            }
        }
    }
}
