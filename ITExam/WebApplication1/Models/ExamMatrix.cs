using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("ExamMatrix")]
    public class ExamMatrix
    {
        [Key]
        public int ExamMatrixId { get; set; }

        [Required(ErrorMessage = "Mã đề là bắt buộc")]
        [ForeignKey("Exam")]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Mã chương là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã chương phải là số dương")]
        public int ChapterId { get; set; }

        [Required(ErrorMessage = "Mã CLO là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Mã CLO phải là số dương")]
        public int CLOId { get; set; }

        [Required(ErrorMessage = "Số lượng câu hỏi là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Số lượng câu hỏi phải từ 1 đến 100")]
        public int QuestionCount { get; set; }

        public Exam Exam { get; set; }
    }
}
