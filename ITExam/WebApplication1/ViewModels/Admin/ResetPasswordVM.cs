using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Admin
{
    public class ResetPasswordVM
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
        public string NewPassword { get; set; } = string.Empty;
    }
}

