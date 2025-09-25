using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("StudentAnswer")]
    public class StudentAnswer
    {
        [Key]
        public int StudentAnswerId { get; set; }

        [Required(ErrorMessage = "Mã câu hỏi là bắt buộc")]
        [ForeignKey("QuestionBank")]
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Mã lịch sử làm bài là bắt buộc")]
        [ForeignKey("TestHistory")]
        public int ExamHistoryId { get; set; }

        public string? EssayAnswer { get; set; }

        public string? MultipleChoiceAnswer { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10")]
        public double? Score { get; set; }

        public QuestionBank QuestionBank { get; set; }
        public ExamHistory ExamHistory { get; set; }
    }
}
