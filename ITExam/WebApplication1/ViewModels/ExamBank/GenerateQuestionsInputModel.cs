namespace ITExam.ViewModels.ExamBank
{
    public class MatrixCloInput
    {
        public int CloId { get; set; }
        public int QuestionCount { get; set; }
    }

    public class MatrixRowInput
    {
        public int ChapterId { get; set; }
        public List<MatrixCloInput> CloInputs { get; set; } = new List<MatrixCloInput>();
    }

    public class GenerateQuestionsInputModel
    {
        public List<MatrixRowInput> MatrixRows { get; set; } = new List<MatrixRowInput>();
    }
}