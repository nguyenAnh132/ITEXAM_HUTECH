using ITExam.ViewModels.Class;

namespace ITExam.ViewModels.Exam
{
    public class ExamHistoryVM
    {
        public DateTime StartTime { get; set; }                  // ThoiGianBatDau
        public DateTime? SubmitTime { get; set; }                // ThoiGianNop
        public double? Score { get; set; }                       // Diem
        public string ExamName { get; set; }                    // TenDe
        public string ExamType { get; set; }                    // TenDe
        public int ExamId { get; set; }                          // MaDe
        public int ClassId { get; set; }                         // MaLopHoc

        public List<ExamHistoryDetailVM> ExamDetails { get; set; } // ChiTietLichSuLamBai
    }
}
