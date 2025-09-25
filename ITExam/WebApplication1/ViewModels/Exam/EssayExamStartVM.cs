namespace ITExam.ViewModels.Exam
{
    public class EssayExamStartVM
    {
        public int ExamId { get; set; }                  // MaDe
        public string ExamName { get; set; }            // TenDe
        public string ExamType { get; set; }             // LoaiDe
        public int UserId { get; set; }
        public string StudentId { get; set; }          // MSSV
        public string FullName { get; set; }             // HoTen
        public int Duration { get; set; }       // ThoiGianLamBai
        public List<EssayQuestionVM>? Questions { get; set; } // DanhSachCauHoi
        public int ClassId { get; set; }                 // MaLop
        public string ClassName { get; set; }            // TenLop
    }

    public class EssayQuestionVM
    {
        public int QuestionId { get; set; }              // MaCauHoi
        public string QuestionContent { get; set; }      // NoiDungCauHoi
        public string? EssayAnswer { get; set; }         // CauTraLoiTuLuan
    }
}
