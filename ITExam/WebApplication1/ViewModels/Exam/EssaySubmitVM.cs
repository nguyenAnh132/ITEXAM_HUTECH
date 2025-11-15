namespace ITExam.ViewModels.Exam
{
    public class EssaySubmitVM
    {
        public int MaDe { get; set; }
        public int MaLop { get; set; }
        public string? UploadedFileId { get; set; } // Dùng để nhận link từ GDrive
    }
}