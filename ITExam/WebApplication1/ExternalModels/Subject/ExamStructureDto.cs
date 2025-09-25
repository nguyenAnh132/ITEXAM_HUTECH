namespace ITExam.ExternalModels.Subject
{
    public class ExamStructureDto
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; }
        public string QuestionType { get; set; }
        public List<AnswerDto> Choices { get; set; } = new();
    }

    public class AnswerDto
    {
        public int ChoiceId { get; set; }
        public string ChoiceContent { get; set; }
        public bool IsSelected { get; set; }
    }
}
