namespace ITExam.ViewModels.Exam
{
    public class MultipleChoiceExamResultVM
    {
        public DateTime ExamDate { get; set; }          // NgayLamBai
        public int Duration { get; set; }        // ThoiGianLamBai
        public int CorrectAnswers { get; set; }         // SoCauDung
        public int QuestionCount { get; set; }         // SoCauHoi
        public double AccuracyRate { get; set; }        // TiLeDung
        public double Score { get; set; }               // Diem
        public int ClassId { get; set; }                // MaLop
    }
}
