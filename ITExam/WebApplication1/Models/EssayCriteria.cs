using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITExam.Models
{
    [Table("EssayCriteria")]
    public class EssayCriterion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public QuestionBank Question { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; }

        [Required]
        [Range(0, 100)]
        public int Percent { get; set; }
    }
}
