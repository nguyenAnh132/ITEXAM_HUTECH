namespace ITExam.ViewModels.Exam
{
    public class ExamListVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public string ExamType { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsUsedInClass { get; set; }
    }

}
