namespace ITExam.ViewModels.Exam
{
    public class GradingVM
    {
        public int ExamId { get; set; }
        public int ClassId { get; set; }
        public int NumberOfUngradedStudents { get; set; } // Số lượng sinh viên chưa làm bài
        public IEnumerable<StudentExamVM> SubmittedStudents { get; set; }   // Đã nộp
        public IEnumerable<StudentExamVM> UnsubmittedStudents { get; set; } // Chưa nộp
    }

    public class StudentExamVM
    {
        public int UserId { get; set; }
        public int? HistoryId { get; set; }
        public string StudentID { get; set; }         // MSSV
        public string FullName { get; set; }          // HoTenSV
        public double? GradedScore { get; set; }      // DiemDaCham
        public DateTime? SubmissionTime { get; set; } // ThoiGianNopBai
        public int? DurationInMinutes { get; set; }   // ThoiGianLamBai
    }

    //public class GradedEssayQuestionVM
    //{
    //    public int QuestionId { get; set; }           // MaCauHoi
    //    public string QuestionContent { get; set; }   // NoiDungCauHoi
    //    public string StudentAnswer { get; set; }     // CauTraLoi
    //    public double? GradedScore { get; set; }       // DiemDuocCham
    //}

    public class EssaySubmissionToGradeVM
    {
        public int HistoryId { get; set; }
        public string FullName { get; set; }
        public string StudentID { get; set; }
        public string ExamTitle { get; set; }
        public DateTime? SubmissionTime { get; set; }
        public string? UploadedFileLink { get; set; }
        public int ExamId { get; set; }
        public int ClassId { get; set; }
        public string? AiOverallComment { get; set; }

        // Danh sách câu hỏi dạng phẳng (Flat List) để dễ Binding khi POST
        // Chúng ta sẽ Group bằng LINQ ở View
        public List<GradedEssayQuestionVM> ListQuestions { get; set; } = new List<GradedEssayQuestionVM>();
    }

    public class GradedEssayQuestionVM
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; }
        public string StudentAnswer { get; set; }

        public double? GradedScore { get; set; } // Điểm sinh viên đạt được (Total)
        public double MaxScore { get; set; }     // Điểm tối đa của câu này (Tính từ Matrix)

        // Thông tin để Group
        public int ChapterId { get; set; }
        public string ChapterTitle { get; set; }
        public int CloId { get; set; }
        public string CloTitle { get; set; }
        public decimal? AiScore { get; set; } 
        public string? AiComment { get; set; }

        // Danh sách tiêu chí (nếu có)
        public List<CriteriaGradingVM> Criteria { get; set; } = new List<CriteriaGradingVM>();
    }

    public class CriteriaGradingVM
    {
        public int CriterionId { get; set; }
        public string Content { get; set; }
        public double Rate { get; set; } // Phần trăm (ví dụ: 0.5 tương ứng 50%)
        public double MaxPoint { get; set; } // Điểm tối đa của tiêu chí này (= Rate * QuestionMaxScore)
        // Không cần lưu điểm tiêu chí vào DB (vì DB chỉ lưu tổng), 
        // nhưng cần property này để UI hiển thị và tính toán
        public double? CurrentPoint { get; set; }
    }
}
