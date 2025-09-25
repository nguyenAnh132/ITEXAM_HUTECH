namespace ITExam.ViewModels.Exam
{
    public class ExamLogEntryVM
    {
        public int LogId { get; set; }
        public string StudentFullName { get; set; }
        public string InstructorName { get; set; }
        public string ExamName { get; set; }
        public string ClassName { get; set; }
        public DateTime LoggedDate { get; set; }
        public string LogContent { get; set; }
    }
}
