using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Class
{
    public class ClassStudentVM
    {
        public string StudentId { get; set; }

        public string FullName { get; set; }

        public string? Email { get; set; }

        public DateTime JoinDate { get; set; }
    }
}
