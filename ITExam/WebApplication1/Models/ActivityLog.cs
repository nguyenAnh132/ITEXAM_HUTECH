using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("ActivityLog")]
    public class ActivityLog //Nhat ky giam sat 
    {
        [Key]
        public int ActivityLogId { get; set; }

        [Required(ErrorMessage = "UserId là bắt buộc")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Mã đề là bắt buộc")]
        [ForeignKey("Exam")]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Mã lớp học là bắt buộc")]
        [ForeignKey("Class")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Họ tên giảng viên là bắt buộc")]
        public string? InstructorName { get; set; }

        [Required(ErrorMessage = "Chuỗi nhật ký là bắt buộc")]
        [Column(TypeName = "nvarchar(max)")]
        public string? LogContent { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Thời điểm ghi nhật ký")]
        public DateTime LogDate { get; set; } = DateTime.Now;

        [InverseProperty("ActivityLogs")]
        public User User { get; set; }

        public Exam Exam { get; set; }
        public Class Class { get; set; }
    }
}
