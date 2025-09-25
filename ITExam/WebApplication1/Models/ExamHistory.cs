using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace ITExam.Models
{
    [Table("ExamHistory")]
    public class ExamHistory
    {
        [Key]
        public int ExamHistoryId { get; set; }

        [Required(ErrorMessage = "UserId là bắt buộc")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu làm bài là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? SubmitTime { get; set; }

        [Required(ErrorMessage = "Thời gian làm bài là bắt buộc")]
        [Range(1, 500, ErrorMessage = "Thời gian làm bài phải từ 1 đến 500 phút")]
        public int Duration { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
        public double? Score { get; set; }

        [Required(ErrorMessage = "Mã đề là bắt buộc")]
        [ForeignKey("Exam")]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Mã lớp học là bắt buộc")]
        [ForeignKey("Class")]
        public int ClassId { get; set; }

        public Exam Exam { get; set; }
        public Class Class { get; set; }
        public User User { get; set; }
        public ICollection<StudentAnswer> StudentAnswers { get; set; }
    }
}
