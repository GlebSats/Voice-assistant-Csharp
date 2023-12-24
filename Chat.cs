using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Speech
{
    // Create chat with openAI chatGPT
    public class Chat
    {
        private List<object> chatHistory;
        private readonly HttpClient gptClient;
        private string CHAT_GPT_KEY = "YOUR GPT KEY";
        public event EventHandler AnswerCompleted;
        public string Answer { get; private set; }
        public Chat()
        {
            gptClient = new HttpClient();
            chatHistory = new List<object>();
            chatHistory.Add(new { role = "assistant", content = "Отвечай коротко и как гопник" }); //First message as chat parameter
            gptClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {CHAT_GPT_KEY}"); // Add Authorization header
        }
        // Sending a request and receiving an answer to the question
        public async Task chatRequest(string question)
        {
            try
            {
                chatHistory.Add(new { role = "assistant", content = question });
                var requestData = new
                {
                    messages = chatHistory,
                    model = "gpt-3.5-turbo",
                    temperature = 0.5,
                    max_tokens = 400
                };
                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                var response = await gptClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonAnswer = await response.Content.ReadAsStringAsync();
                    JObject jobject = JObject.Parse(jsonAnswer);
                    Answer = jobject["choices"][0]["message"]["content"].ToString();
                    AnswerCompleted?.Invoke(this, null);
                } else
                {
                    throw new Exception($"HTTP error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Chat request error: {ex.Message}");
            }
        }
    }
}
