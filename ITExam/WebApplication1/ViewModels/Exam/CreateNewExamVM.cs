using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Exam
{
    public class CreateNewExamVM
    {
        public int ExamBankId { get; set; }
        public string ExamBankName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
    }
}
