using System.ComponentModel.DataAnnotations;

namespace ITExam.ViewModels.Exam
{
    public class ExamUnlockTimeVM
    {
        [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc.")]
        public DateTime EndTime { get; set; }

        public bool LaDeThi { get; set; }
    }
}
