using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Admin
{
    public class TeacherCreateVM
    {
        [Required]
        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Faculty { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}

