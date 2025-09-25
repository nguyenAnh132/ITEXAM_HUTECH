using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("Exam")]
    public class Exam
    {
        [Key]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Tên đề thi là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên đề thi không được vượt quá 200 ký tự")]
        public string ExamName { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; }

        [Required(ErrorMessage = "Thời gian làm bài là bắt buộc")]
        [Range(1, 500, ErrorMessage = "Thời gian làm bài phải từ 1 đến 500 phút")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Loại đề là bắt buộc")]
        [StringLength(50, ErrorMessage = "Loại đề không được vượt quá 50 ký tự")]
        public string ExamType { get; set; } 

        [Required(ErrorMessage = "Ngân hàng đề là bắt buộc")]
        [ForeignKey("ExamBank")]
        public int ExamBankId { get; set; }

        [Required(ErrorMessage = "Người tạo đề là bắt buộc")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public ExamBank ExamBank { get; set; }
        public User User { get; set; }
        public ICollection<ClassExam> ClassExams { get; set; }
        public ICollection<ExamMatrix> ExamMatrices { get; set; }
        public ICollection<ExamHistory> ExamHistories { get; set; }
    }
}
