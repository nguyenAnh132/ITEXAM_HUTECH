namespace ITExam.ViewModels.Class
{
    public class StudentSubmissionInClassVM
    {
        public string StudentId { get; set; }

        public string FullName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime SubmitTime { get; set; }
        public int Duration { get; set; }
        public float Score { get; set; }
        public int ExamHistoryId { get; set; }
    }
}
