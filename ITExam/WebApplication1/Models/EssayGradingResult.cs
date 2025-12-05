using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITExam.Models;

[Table("EssayGradingResult")]
public class EssayGradingResult
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ExamHistoryId { get; set; }

    [ForeignKey("ExamHistoryId")] 
    public ExamHistory ExamHistory { get; set; }

    // Điểm tổng kết bài thi tự luận
    public decimal TotalScore { get; set; }

    // Nhận xét chung của AI
    [Column(TypeName = "nvarchar(max)")]
    public string? OverallComment { get; set; }

    [Required]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
