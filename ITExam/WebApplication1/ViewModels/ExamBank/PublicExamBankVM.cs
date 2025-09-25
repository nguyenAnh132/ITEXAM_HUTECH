namespace ITExam.ViewModels.ExamBank
{
    public class PublicExamBankVM
    {
        public int ExamBankId { get; set; }
        public string ExamBankName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ExamType { get; set; }
        public int LecturerId { get; set; }
        public string? LecturerName { get; set; }
    }
}