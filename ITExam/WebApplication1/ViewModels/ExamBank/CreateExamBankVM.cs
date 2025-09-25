using ITExam.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.ViewModels.ExamBank
{
    public class CreateExamBankVM
    {
        public string ExamBankName { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Status { get; set; }

        public int SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }

        public string ExamType { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
