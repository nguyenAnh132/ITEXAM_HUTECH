using ITExam.Models;
using ITExam.ViewModels.Exam;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ITExam.ViewModels.Class
{
    public class ClassDetailVM
    {
        public Models.Class ClassInfo { get; set; }
        public List<ClassStudentVM> Students { get; set; }
        public List<ClassExamVM> Exams { get; set; }
        public List<UnassignedExamVM> UnassignedExams { get; set; }
        public List<ExamLogEntryVM> Logs { get; set; }
        public int StudentCount { get; set; }
        public int ExamCount { get; set; }
        public string ClassCode { get; set; }
    }
}
