using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("ExamBank")]
    public class ExamBank
    {
        [Key]
        public int ExamBankId { get; set; }

        [Required(ErrorMessage = "Tên ngân hàng đề là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên ngân hàng đề không được vượt quá 200 ký tự")]
        public string ExamBankName { get; set; }

        [Required(ErrorMessage = "Ngày tạo là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; }

        public bool Status { get; set; }

        [Required(ErrorMessage = "Mã môn học là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã môn học phải là số dương")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Loại đề là bắt buộc")]
        [StringLength(50, ErrorMessage = "Loại đề không được vượt quá 50 ký tự")]
        public string ExamType { get; set; }

        [Required(ErrorMessage = "Người tạo là bắt buộc")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public User User { get; set; }
        public ICollection<QuestionBank> QuestionBanks { get; set; }
        public ICollection<Exam> Exams { get; set; }
    }
}

