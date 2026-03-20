using ITExam.ExternalModels.Subject;

namespace ITExam.Services
{
    public interface ISubjectService
    {
        Task<List<SubjectDto>> GetSubjectsAsync();
        Task<SubjectDto?> GetSubjectByIdAsync(int subjectId);
    }
}

