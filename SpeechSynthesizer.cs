using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Speech
{
    // Speech synthesizer using Yandex API
    public class SpeechSynthesizer
    {
        private readonly HttpClient yandexClient;
        private string YANDEX_OAuth = "YOUR YANDEX KEY";
        private string yaTokenIAM;
        private string FOLDER_ID = "YOUR FOLDER ID";
        public event EventHandler SpeechSynthesisCompleted;
        public SpeechSynthesizer()
        {
            yandexClient = new HttpClient();
        }
        // Getting authorization token and adding header
        public async Task Init()
        {
            try
            {
                var requestData = new
                {
                    yandexPassportOauthToken = YANDEX_OAuth
                };
                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var yaResponse = await yandexClient.PostAsync("https://iam.api.cloud.yandex.net/iam/v1/tokens", content);
                if (yaResponse.IsSuccessStatusCode)
                {
                    var yaAnswer = await yaResponse.Content.ReadAsStringAsync();
                    JObject yaJobject = JObject.Parse(yaAnswer);
                    yaTokenIAM = yaJobject["iamToken"].ToString();
                    yandexClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {yaTokenIAM}");
                } else
                {
                    throw new Exception($"HTTP error: {yaResponse.StatusCode}");
                }  
            }
            catch (Exception ex)
            {
                throw new Exception($"Yandex getting token error: {ex.Message}");
            }
        }
        // Setting parameters and getting audio file 
        public async Task speechSynthesis(string textToSynthesis, string outputFilePath)
        {
            try
            {
                var requestData = new Dictionary<string, string>
            {
                { "text", textToSynthesis },
                { "voice", "ermil" },
                { "emotion", "good" },
                { "format", "mp3" },
                { "folderId", FOLDER_ID }
            };
                var content = new FormUrlEncodedContent(requestData);
                var response = await yandexClient.PostAsync("https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize", content);
                if (response.IsSuccessStatusCode)
                {
                    var mp3File = await response.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(outputFilePath, mp3File);
                    SpeechSynthesisCompleted?.Invoke(this, null);
                } else
                {
                    throw new Exception($"HTTP error: {response.StatusCode}");
                }  
            }
            catch (Exception ex)
            {
                throw new Exception($"Yandex speech synthesis error: {ex.Message}");
            }  
        }
    }
}
