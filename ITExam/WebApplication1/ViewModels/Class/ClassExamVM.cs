using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Class
{
    public class ClassExamVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public bool Access { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Duration { get; set; }
        public DateTime? AddedDate { get; set; }
        public string ExamType { get; set; }
        public int? QuestionCount { get; set; }
        public int? ClassId { get; set; }
        public string? ClassName { get; set; }
        public int? UngradedStudentCount { get; set; }
        public bool IsDone { get; set; }
        public bool IsDoingExam { get; set; }

    }
}
