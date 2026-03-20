using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Admin
{
    public class ChapterEditVM
    {
        [Required]
        public int ChapterId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;
    }
}

