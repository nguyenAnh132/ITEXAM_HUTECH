using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Class
{
    public class ClassInfoVM
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; }

        public string ClassCode { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? Description { get; set; }

        public int UserId { get; set; }

        public string UserFullName { get; set; }
    }
}
