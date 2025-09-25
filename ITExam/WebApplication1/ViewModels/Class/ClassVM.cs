using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.ViewModels.Class
{
    public class ClassVM
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Today;
        public string? Description { get; set; }
        public int TeacherId { get; set; }
        public int StudentCount { get; set; }
        public int ExamCount { get; set; }
    }
}
