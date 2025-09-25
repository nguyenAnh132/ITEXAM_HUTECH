using Newtonsoft.Json;
using System.Collections.Generic;

namespace ITExam.ExternalModels
{
    public class ApiResponse
    {
        [JsonProperty("candidates")]
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonProperty("content")]
        public Content Content { get; set; }
    }

    public class Content
    {
        [JsonProperty("parts")]
        public List<Part> Parts { get; set; }
    }

    public class Part
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class DatabaseQuestion
    {
        [JsonProperty("questionContent")]
        public string QuestionContent { get; set; }

        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }
        public int ChapterId { get; set; }
        public int CloId { get; set; }
        public int CorrectChoiceId { get; set; }
    }

    public class Choice
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("NoiDung")]
        public string NoiDung { get; set; }

        [JsonProperty("LaDapAn")]
        public bool LaDapAn { get; set; }
    }
}