using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Admin
{
    public class SubjectCreateVM
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;
    }
}

