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

    public class GradedEssayQuestionVM
    {
        public int QuestionId { get; set; }           // MaCauHoi
        public string QuestionContent { get; set; }   // NoiDungCauHoi
        public string StudentAnswer { get; set; }     // CauTraLoi
        public double? GradedScore { get; set; }       // DiemDuocCham
    }

    public class EssaySubmissionToGradeVM
    {
        public int HistoryId { get; set; }            // LichSuId
        public string FullName { get; set; }          // HoTenSV
        public string StudentID { get; set; }       // MSSV
        public string ExamTitle { get; set; }         // TenDeThi
        public DateTime? SubmissionTime { get; set; } // ThoiGianNop
        public List<GradedEssayQuestionVM> ListQuestions { get; set; } // DanhSachCauHoi
    }
}
