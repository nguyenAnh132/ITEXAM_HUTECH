using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace ITExam.Models
{
    [Table("ClassDetail")]
    public class ClassDetail
    {
        [Required(ErrorMessage = "Mã người dùng là bắt buộc")]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Mã lớp học là bắt buộc")]
        [ForeignKey("Class")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Ngày tham gia là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime JoinDate { get; set; }

        public User User { get; set; }
        public Class Class { get; set; }
    }
}
