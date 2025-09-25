using ITExam.ExternalModels.Subject;
using ITExam.ExternalModels;

namespace ITExam.ViewModels.ExamBank
{
    public class ReviewQuestionsVM
    {
        public int ExamBankId { get; set; }
        public SubjectDto SubjectData { get; set; }
        public List<DatabaseQuestion> GeneratedQuestions { get; set; }
    }
}
