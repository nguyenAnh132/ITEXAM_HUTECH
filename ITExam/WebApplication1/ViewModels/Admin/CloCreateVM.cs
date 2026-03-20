using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Admin
{
    public class CloCreateVM
    {
        [Required]
        public int ChapterId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }
    }
}

