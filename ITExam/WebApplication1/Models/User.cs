using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Tên đăng nhập không được vượt quá 255 ký tự")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự")]
        public string FullName { get; set; }

        [MaxLength(100, ErrorMessage = "Tên lớp không được vượt quá 100 ký tự")]
        public string? ClassName { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [MaxLength(50, ErrorMessage = "Vai trò không được vượt quá 50 ký tự")]
        public string Role { get; set; }

        [Required(ErrorMessage = "AccessToken là bắt buộc")]
        [MaxLength(500)]
        public string AccessToken { get; set; }

        [MaxLength(255, ErrorMessage = "Tên khoa không được vượt quá 255 ký tự")]
        public string? Faculty { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        public ICollection<ClassDetail> ClassDetails { get; set; }
        public ICollection<Class> Classes { get; set; }
        public ICollection<ExamBank> ExamBanks { get; set; }
        public ICollection<ExamHistory> ExamHistories { get; set; }
        public ICollection<Exam> Exams { get; set; }
        public ICollection<ActivityLog> ActivityLogs { get; set; }
    }
}
