using ITExam.Models;
using ITExam.ViewModels.Admin;

namespace ITExam.Services
{
    public interface ISubjectAdminService
    {
        Task<List<Subject>> GetSubjectsAsync();
        Task<Subject?> GetSubjectWithChaptersAsync(int subjectId);

        Task CreateSubjectAsync(SubjectCreateVM vm);
        Task UpdateSubjectAsync(SubjectEditVM vm);
        Task DeleteSubjectAsync(int subjectId);

        Task<List<SubjectChapter>> GetChaptersWithClosAsync(int subjectId);
        Task CreateChapterAsync(ChapterCreateVM vm);
        Task UpdateChapterAsync(ChapterEditVM vm);
        Task DeleteChapterAsync(int chapterId);

        Task<List<SubjectClo>> GetClosByChapterAsync(int chapterId);
        Task CreateCloAsync(CloCreateVM vm);
        Task UpdateCloAsync(CloEditVM vm);
        Task DeleteCloAsync(int cloId);
    }
}

