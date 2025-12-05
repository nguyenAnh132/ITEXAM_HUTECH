
using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Exam
{
    public class ExamVM
    {
        public int ExamId { get; set; }          // MaDe
        public string ExamName { get; set; }      // TenDe
        public string FullName { get; set; }    // HoTen
        public DateTime CreatedDate { get; set; }      // NgayTao
        public string ExamType { get; set; }   // Loaide
        public int? QuestionCount { get; set; }  // SoLuongCauHoi

        
        [Range(1, 299, ErrorMessage = "Thời gian làm bài phải từ 1 đến 299 phút.")]
        public int Duration { get; set; }   // ThoiGianLamBai

        public int ClassId { get; set; }     // MaLop
    }
}
