namespace ITExam.ViewModels.Exam
{
    public class StudentExamHistoryVM
    {
        public int ClassId { get; set; }                 // MaLopHoc
        public int ExamId { get; set; }                  // MaDeThi
        public int ExamHistoryId { get; set; }           // MaLichSuLamBai
        public string ClassName { get; set; }            // TenLopHoc
        public string ExamName{ get; set; }            // TenDeThi
        public DateTime StartTime { get; set; }          // ThoiGianBatDau
        public DateTime? SubmissionTime { get; set; }    // ThoiGianNop
        public double? Score { get; set; }               // Diem
        public bool IsExam { get; set; }         // LaDeThi - Phân biệt đề thi hay đề ôn
        public bool Access { get; set; }                 // Có được truy cập vào đề không
    }

    public class UpdateAccessVM
    {
        public int ClassId { get; set; }                 // MaLopHoc
        public int ExamId { get; set; }                  // MaDe
        public bool Access { get; set; }                 // Trạng thái truy cập
    }
}
