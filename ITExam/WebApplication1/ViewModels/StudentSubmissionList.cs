using ITExam.Models;
using ITExam.ViewModels.Class;

namespace ITExam.ViewModels
{
    public class StudentSubmissionList
    {
        public Models.Class ClassInfo { get; set; }
        public List<StudentSubmissionInClassVM> Students { get; set; }
    }
}
