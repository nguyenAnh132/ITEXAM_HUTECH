namespace ITExam.ViewModels.Exam
{
    public class ExamVM
    {
        public int ExamId { get; set; }                 // MaDe
        public string ExamName { get; set; }           // TenDe
        public string FullName { get; set; }     // HoTen
        public DateTime CreatedDate { get; set; }       // NgayTao
        public string ExamType { get; set; }            // Loaide
        public int? QuestionCount { get; set; }         // SoLuongCauHoi
        public int Duration { get; set; }        // ThoiGianLamBai
        public int ClassId { get; set; }                // MaLop
    }
}
