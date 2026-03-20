using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Admin
{
    public class ChapterCreateVM
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;
    }
}

