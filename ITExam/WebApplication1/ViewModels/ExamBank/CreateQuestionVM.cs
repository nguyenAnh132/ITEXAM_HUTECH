using ITExam.ExternalModels.Subject;
using ITExam.Models;
using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.ExamBank
{
    public class CreateQuestionVM
    {
        public int? QuestionId { get; set; }

        [Required]
        public int ExamBankId { get; set; }

        public string ExamBankName { get; set; }

        public bool IsActive { get; set; }

        [Required]
        public string QuestionContent { get; set; }

        public string? QuestionType { get; set; }

        public int? CLOId { get; set; }

        public int? ChapterId { get; set; }

        public double? QuestionScore { get; set; }

        public List<string> ChoiceContents { get; set; } = new List<string>();

        public List<string> CorrectAnswers { get; set; } = new List<string>();

        public List<QuestionBank> QuestionList { get; set; } = new List<QuestionBank>();

        public List<CreateChoiceVM> AnswerChoices { get; set; } = new List<CreateChoiceVM>();

        public List<ChapterDto> ChapterList { get; set; } = new List<ChapterDto>();

        public List<QuestionDto> QuestionDTOList { get; set; } = new List<QuestionDto>();
    }

    public class CreateChoiceVM
    {
        public int ChoiceId { get; set; }

        public string ChoiceContent { get; set; }

        public bool? IsAnswer { get; set; }

        public int QuestionId { get; set; }
    }
}
