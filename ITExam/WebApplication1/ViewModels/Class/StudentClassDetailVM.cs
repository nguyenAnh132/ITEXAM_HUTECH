using ITExam.ViewModels.Exam;

namespace ITExam.ViewModels.Class
{
    public class StudentClassDetailVM
    {
        public StudentClassInfoVM ClassInfo { get; set; }
        public List<ClassStudentVM> Students { get; set; }
        public List<StudentClassExamVM> Exams { get; set; }
        public int StudentCount { get; set; }
        public int ExamCount { get; set; }
    }
    public class StudentClassInfoVM
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; }

        public string? Description { get; set; }

        public string InstructorName { get; set; }

    }
}
