using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("SubjectChapter")]
    public class SubjectChapter
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Chapter title is required")]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public Subject? Subject { get; set; }

        public ICollection<SubjectClo> Clos { get; set; } = new List<SubjectClo>();
    }
}

