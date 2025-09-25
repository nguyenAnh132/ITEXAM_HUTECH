using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Class
{
    public class UnassignedExamVM
    {
        public int ExamId { get; set; }
        public int InstructorId { get; set; }

        public string ExamName { get; set; }

        [Required]
        public int Duration { get; set; }

        public DateTime CreatedDate { get; set; }
        public int QuestionCount { get; set; }
    }
}
