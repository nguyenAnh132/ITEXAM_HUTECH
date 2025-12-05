using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("EssayGradingDetail")]
public class EssayGradingDetail
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GradingResultId { get; set; }

    [ForeignKey("GradingResultId")]
    public EssayGradingResult GradingResult { get; set; }

    [Required]
    public int QuestionId { get; set; }

    // Điểm AI chấm cho câu này
    public decimal Score { get; set; }

    // Nhận xét chi tiết của AI cho câu hỏi
    [Column(TypeName = "nvarchar(max)")]
    public string? Comment { get; set; }
}
