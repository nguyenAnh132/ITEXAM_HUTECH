using ITExam.ExternalModels.Subject;
using Newtonsoft.Json;

namespace ITExam.ViewModels
{
    public class ExamBankDetailVM
    {
        public int ExamBankId { get; set; }
        public string ExamBankName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ExamType { get; set; }
        public List<ExamBankQuestionVM> ExamBankQuestions { get; set; }
        public SubjectDto Subject { get; set; }
    }

    public class ExamBankQuestionVM
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; }
        public string? Options { get; set; }
        public int? CloId { get; set; }
        public int? ChapterId { get; set; }
        public string? QuestionType { get; set; }
        public double? QuestionScore { get; set; }
    }

    public class QuestionOptionVM
    {
        [JsonProperty("Id")]
        public int ChoiceId { get; set; }
        [JsonProperty("NoiDung")]
        public string? ChoiceContent { get; set; }
        [JsonProperty("LaDapAn")]
        public bool IsCorrectAnswer { get; set; }
    }
}