using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace ITExam.Models
{
    [Table("ClassExam")]
    public class ClassExam
    {
        [Required(ErrorMessage = "Mã lớp học là bắt buộc")]
        [ForeignKey("Class")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Mã đề thi là bắt buộc")]
        [ForeignKey("Exam")]
        public int ExamId { get; set; }

        public bool IsExam { get; set; } //Sua sau

        public bool? Access { get; set; }

        [Required(ErrorMessage = "Ngày thêm là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime AddedDate { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        public Exam Exam { get; set; }
        public Class Class { get; set; }
    }
}
