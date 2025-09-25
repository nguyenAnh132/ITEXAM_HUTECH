using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("Class")]
    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Tên lớp học là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên lớp học không được vượt quá 200 ký tự")]
        public string ClassName { get; set; }

        [Required(ErrorMessage = "Mã lớp học là bắt buộc")]
        public string ClassCode { get; set; }

        [Required(ErrorMessage = "Ngày tạo là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; }

        [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Người tạo lớp là bắt buộc")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public User User { get; set; }
        public ICollection<ClassDetail> ClassDetails { get; set; } 
        public ICollection<ClassExam> ClassExams { get; set; }
        public ICollection<ExamHistory> ExamHistories { get; set; }
    }
}
