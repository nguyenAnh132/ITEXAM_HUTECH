using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels
{
    public class LoginVM
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

    }
}
