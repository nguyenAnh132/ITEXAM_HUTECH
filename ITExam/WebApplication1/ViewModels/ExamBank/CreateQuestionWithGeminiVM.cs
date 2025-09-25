using System.Collections.Generic;

namespace ITExam.ViewModels.ExamBank
{
    public class CreateQuestionWithGeminiVM
    {
        public int ExamBankId { get; set; }
        public string ExamBankName { get; set; }
        public string SubjectName { get; set; }
        public List<ChapterCLOVM> Chapters { get; set; } = new List<ChapterCLOVM>();
    }
    public class ChapterCLOVM
    {
        public int ChapterId { get; set; }

        public string ChapterName { get; set; }

        public List<CLOItem> Clos { get; set; } = new List<CLOItem>();
    }

    public class CLOItem
    {
        public int CLOId { get; set; }

        public string CLOTitle { get; set; }

        public string CLODescription { get; set; }
    }
}