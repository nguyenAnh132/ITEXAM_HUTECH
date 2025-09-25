using ITExam.ExternalModels;
using Newtonsoft.Json;
using System.Text;

namespace ITExam.Services
{
    public class GeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GeminiService(IHttpClientFactory httpClientFactory, string apiKey)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<DatabaseQuestion>> GetQuestionsFromGeminiAsync(string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            var url = RoutingAPI.LLMApiUrl;

            #region Tạo Request Body
            var requestBody = new
            {
                contents = new[] {
            new {
                parts = new[] {
                    new {
                        text = prompt
                    }
                }
            }
        },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "ARRAY",
                        items = new
                        {
                            type = "OBJECT",
                            properties = new
                            {
                                questionContent = new { type = "STRING" },
                                choices = new
                                {
                                    type = "ARRAY",
                                    items = new
                                    {
                                        type = "OBJECT",
                                        properties = new
                                        {
                                            Id = new { type = "INTEGER" },
                                            NoiDung = new { type = "STRING" },
                                            LaDapAn = new { type = "BOOLEAN" }
                                        },
                                        required = new[] { "Id", "NoiDung", "LaDapAn" }
                                    }
                                },
                            },
                            required = new[] { "questionContent", "choices" }
                        }
                    }
                }
            };
            #endregion

            var jsonRequest = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi khi gọi API Gemini: {response.StatusCode} - {response.ReasonPhrase}. Chi tiết: {errorContent}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Dữ liệu trả về từ API: " + responseString);
            var geminiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseString);
            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0 ||
                geminiResponse.Candidates[0].Content?.Parts == null || geminiResponse.Candidates[0].Content.Parts.Count == 0 ||
                string.IsNullOrEmpty(geminiResponse.Candidates[0].Content.Parts[0].Text))
            {
                Console.WriteLine("Phản hồi từ API không chứa dữ liệu câu hỏi hợp lệ.");
                return new List<DatabaseQuestion>();
            }
            var questionJsonString = geminiResponse.Candidates[0].Content.Parts[0].Text;
            try
            {
                var questions = JsonConvert.DeserializeObject<List<DatabaseQuestion>>(questionJsonString);
                return questions;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Lỗi khi phân tích JSON chứa câu hỏi: {ex.Message}");
                throw new Exception("Không thể phân tích dữ liệu câu hỏi từ phản hồi của API.", ex);
            }
        }
    }
}
