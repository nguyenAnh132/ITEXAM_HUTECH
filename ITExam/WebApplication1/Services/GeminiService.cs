using System.Text;
using ITExam.ExternalModels;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace ITExam.Services
{
    public class GeminiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RoutingAPI _routingApi;

        public GeminiService(IHttpClientFactory httpClientFactory, IOptions<RoutingAPI> routingOptions)
        {
            _httpClientFactory = httpClientFactory;
            _routingApi = routingOptions.Value;
        }

        public async Task<List<DatabaseQuestion>> GetQuestionsFromGeminiAsync(string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            var url = _routingApi.LLMApiUrl;

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
            var geminiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseString);
            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0 ||
                geminiResponse.Candidates[0].Content?.Parts == null || geminiResponse.Candidates[0].Content.Parts.Count == 0 ||
                string.IsNullOrEmpty(geminiResponse.Candidates[0].Content.Parts[0].Text))
            {
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
                throw new Exception("Không thể phân tích dữ liệu câu hỏi từ phản hồi của API.", ex);
            }
        }

        public async Task<List<EssayGradingDetail>> GadingEssayExamAsync(string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            var url = _routingApi.LLMApiUrl;

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
                                questionId = new { type = "INTEGER" },
                                score = new { type = "NUMBER" },
                                comment = new { type = "STRING" }
                            },
                            required = new[] { "questionId", "score", "comment" }
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
            var geminiResponse = JsonConvert.DeserializeObject<EssayGradingApiResponse>(responseString);
            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0 ||
                geminiResponse.Candidates[0].Content?.Parts == null || geminiResponse.Candidates[0].Content.Parts.Count == 0 ||
                string.IsNullOrEmpty(geminiResponse.Candidates[0].Content.Parts[0].Text))
            {
                return new List<EssayGradingDetail>();
            }
            var questionJsonString = geminiResponse.Candidates[0].Content.Parts[0].Text;
            try
            {
                var questions = JsonConvert.DeserializeObject<List<EssayGradingDetail>>(questionJsonString);
                return questions;
            }
            catch (JsonException ex)
            {
                throw new Exception("Không thể phân tích dữ liệu câu hỏi từ phản hồi của API.", ex);
            }
        }
    }
}
