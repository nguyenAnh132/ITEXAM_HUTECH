namespace ITExam.ViewModels.Exam
{
    public class ExamHistoryDetailVM
    {
        public string QuestionContent { get; set; }          // NoiDungCauHoi
        public string ChoiceContent { get; set; }            // NoiDungLuaChon
        public string QuestionType { get; set; }             // LoaiCauHoi
        public string MultipleChoiceAnswer { get; set; }     // CauTraLoiTN
        public string EssayAnswer { get; set; }              // CauTraLoiTuLuan
    }
}
