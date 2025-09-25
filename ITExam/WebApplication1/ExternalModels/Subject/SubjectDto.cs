namespace ITExam.ExternalModels.Subject
{
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public List<ChapterDto> Chapters { get; set; }
    }

    public class ChapterDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Index { get; set; }
        public List<CloDto> Clos { get; set; }
    }

    public class CloDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class QuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; }
        public string ChoiceContent { get; set; }
        public string? QuestionType { get; set; }
        public int CloId { get; set; }
        public int ChapterId { get; set; }
    }
}
