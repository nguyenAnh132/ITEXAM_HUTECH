using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("Subject")]
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject code is required")]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject name is required")]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        public ICollection<SubjectChapter> Chapters { get; set; } = new List<SubjectChapter>();
    }
}

