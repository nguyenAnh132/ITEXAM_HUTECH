using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Admin
{
    public class TeacherEditVM
    {
        [Required]
        public int UserId { get; set; }

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

        [MinLength(6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
        public string? NewPassword { get; set; }
    }
}

