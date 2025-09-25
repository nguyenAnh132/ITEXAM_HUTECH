using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Class
{
    public class ClassExamVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public bool Type { get; set; }
        public bool Access { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Duration { get; set; }
        public DateTime? ExamDate { get; set; }
        public string ExamType { get; set; }
        public int? QuestionCount { get; set; }
        public int? ClassId { get; set; }
        public string? ClassName { get; set; }
        public int? UngradedStudentCount { get; set; }
        public bool IsDone { get; set; }

    }
}
