using System.Text.Json.Serialization;

namespace ITExam.ViewModels.Exam
{
    public class MultipleChoiceExamEntryVM
    {
        public int ExamId { get; set; }                   // MaDe
        public string ExamName { get; set; }             // TenDe
        public string ExamType { get; set; }              // LoaiDe
        public int Duration { get; set; }        // ThoiGianLamBai
        public int UserId { get; set; }                   // UserId
        public string StudentId { get; set; }           // MSSV
        public string FullName { get; set; }              // HoTen
        public List<MultipleChoiceQuestionVM>? Questions { get; set; } // DanhSachCauHoi
        public int ClassId { get; set; }                  // MaLop
        public string ClassName { get; set; }             // TenLop
    }

    public class MultipleChoiceQuestionVM
    {
        public int QuestionId { get; set; }               // MaCauHoi
        public string QuestionContent { get; set; }       // NoiDungCauHoi
        public string QuestionType { get; set; }          // LoaiCauHoi
        public List<ChoiceVM> Choices { get; set; }       // DanhSachLuaChon
    }

    public class ChoiceVM
    {
        public int ChoiceId { get; set; }                 // MaLuaChon
        public string ChoiceContent { get; set; }         // NoiDungLuaChon
        public bool IsSelected { get; set; }              // Chon
    }
}
