using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.ExamBank
{
    public class PrivateExamBankVM
    {
        public int ExamBankId { get; set; }
        [StringLength(255)]
        public string ExamBankName { get; set; }
        public string ExamType { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}