namespace ITExam.ExternalModels.Subject
{
    public class DeleteLogsDto
    {
        public int ClassId { get; set; }
        public List<int> LogIds { get; set; } = new();
    }
}
