namespace ITExam.ViewModels.Exam
{
    public class ExamMatrixVM
    {
        public int ExamId { get; set; } 
        public string ExamName { get; set; }
        public List<ChapterCLOVM> Chapters { get; set; }
        public int Duration { get; set; }

    }

    public class ChapterCLOVM
    {
        public int ChapterId { get; set; }
        public string ChapterTitle { get; set; }
        public List<CLOItem> CLOs { get; set; }
    }

    public class CLOItem
    {
        public int CLOId { get; set; } 
        public string CLO { get; set; }
        public int QuestionCount { get; set; }
        public int MaxQuestionCount { get; set; }
    }

}
