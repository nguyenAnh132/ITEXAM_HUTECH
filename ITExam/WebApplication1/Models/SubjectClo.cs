using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("SubjectClo")]
    public class SubjectClo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "CLO title is required")]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public int ChapterId { get; set; }

        [ForeignKey(nameof(ChapterId))]
        public SubjectChapter? Chapter { get; set; }
    }
}

