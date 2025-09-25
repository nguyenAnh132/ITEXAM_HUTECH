namespace ITExam.ViewModels.Exam
{
    public class StudentVM
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string? Ip { get; set; }
        public string? Brower { get; set; }
    }

    public class MonitoringVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public string ExamType { get; set; }
        public int Duration { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<StudentVM> StudentList { get; set; }
    }

}
