using Newtonsoft.Json;
using System.Collections.Generic;

namespace ITExam.ExternalModels
{
    public class EssayGradingApiResponse
    {
        [JsonProperty("candidates")]
        public List<EssayCandidate> Candidates { get; set; }
    }

    public class EssayCandidate
    {
        [JsonProperty("content")]
        public EssayContent Content { get; set; }
    }

    public class EssayContent
    {
        [JsonProperty("parts")]
        public List<EssayPart> Parts { get; set; }
    }

    public class EssayPart
    {
        // Đây là JSON string chứa mảng kết quả chấm
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    // JSON mảng kết quả chấm
    public class EssayGradingItem
    {
        [JsonProperty("questionId")]
        public int QuestionId { get; set; }

        [JsonProperty("score")]
        public decimal Score { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}
