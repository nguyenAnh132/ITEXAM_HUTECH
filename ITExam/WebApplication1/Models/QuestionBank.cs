using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("QuestionBank")]
    public class QuestionBank
    {
        [Key]
        public int QuestionId { get; set; } //Ma cau hoi ngan hang de

        [Required(ErrorMessage = "Nội dung câu hỏi là bắt buộc")]
        [Column(TypeName = "nvarchar(max)")]
        public string QuestionContent { get; set; } //Noi dung cau hoi

        [Column(TypeName = "nvarchar(max)")]
        public string? ChoiceContent { get; set; } //Noi dung cac lua chon (dành cho câu hỏi trắc nghiệm)

        public int? CLOId { get; set; } //CLO cua cau hoi

        public int? ChapterId { get; set; } //Chuong cau hoi

        public string? QuestionType { get; set; } //Loai cau hoi (ví dụ: "Trắc nghiệm", "Tự luận", "Điền khuyết")

        [ForeignKey("ExamBank")]
        [Required(ErrorMessage = "Mã ngân hàng đề là bắt buộc")]
        public int ExamBankId { get; set; } // MaNHD (Ma ngan hang de)

        [Range(0, 10, ErrorMessage = "Điểm câu hỏi phải từ 0 đến 10")]
        public double? QuestionScore { get; set; } // Chỉ áp dụng cho tự luận

        public ExamBank ExamBank { get; set; } // Cha

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
    }
}
