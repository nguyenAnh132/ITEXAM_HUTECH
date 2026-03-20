using ITExam.Models;
using ITExam.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace ITExam.Services
{
    public class SubjectAdminService : ISubjectAdminService
    {
        private readonly ITExamDbContext _db;

        public SubjectAdminService(ITExamDbContext db)
        {
            _db = db;
        }

        public async Task<List<Subject>> GetSubjectsAsync()
        {
            return await _db.Subjects
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToListAsync();
        }

        public async Task<Subject?> GetSubjectWithChaptersAsync(int subjectId)
        {
            return await _db.Subjects
                .Include(s => s.Chapters)
                .ThenInclude(ch => ch.Clos)
                .SingleOrDefaultAsync(s => s.Id == subjectId);
        }

        public async Task CreateSubjectAsync(SubjectCreateVM vm)
        {
            if (await _db.Subjects.AnyAsync(s => s.Code.ToLower() == vm.Code.ToLower()))
            {
                throw new InvalidOperationException("Mã môn học đã tồn tại.");
            }

            var subject = new Subject
            {
                Code = vm.Code.Trim(),
                Name = vm.Name.Trim()
            };

            _db.Subjects.Add(subject);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateSubjectAsync(SubjectEditVM vm)
        {
            var subject = await _db.Subjects.SingleOrDefaultAsync(s => s.Id == vm.Id);
            if (subject == null)
            {
                throw new InvalidOperationException("Không tìm thấy môn học.");
            }

            var codeExists = await _db.Subjects.AnyAsync(s =>
                s.Id != vm.Id && s.Code.ToLower() == vm.Code.ToLower());

            if (codeExists)
            {
                throw new InvalidOperationException("Mã môn học đã tồn tại.");
            }

            subject.Code = vm.Code.Trim();
            subject.Name = vm.Name.Trim();
            subject = _db.Subjects.Update(subject).Entity;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteSubjectAsync(int subjectId)
        {
            var subject = await GetSubjectWithChaptersAsync(subjectId);
            if (subject == null) throw new InvalidOperationException("Không tìm thấy môn học.");

            bool usedInExamBanks = await _db.ExamBanks.AnyAsync(eb => eb.SubjectId == subjectId);
            if (usedInExamBanks)
            {
                throw new InvalidOperationException("Không thể xóa môn học do đang được sử dụng trong ngân hàng đề.");
            }

            var chapterIds = subject.Chapters.Select(c => c.Id).ToList();
            var cloIds = subject.Chapters.SelectMany(c => c.Clos).Select(c => c.Id).ToList();

            await EnsureChapterOrCloNotUsedAsync(chapterIds, cloIds);

            _db.Subjects.Remove(subject);
            await _db.SaveChangesAsync();
        }

        public async Task<List<SubjectChapter>> GetChaptersWithClosAsync(int subjectId)
        {
            return await _db.SubjectChapters
                .AsNoTracking()
                .Where(ch => ch.SubjectId == subjectId)
                .Include(ch => ch.Clos)
                .OrderBy(ch => ch.Id)
                .ToListAsync();
        }

        public async Task CreateChapterAsync(ChapterCreateVM vm)
        {
            var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == vm.SubjectId);
            if (!subjectExists) throw new InvalidOperationException("Không tìm thấy môn học.");

            var chapter = new SubjectChapter
            {
                SubjectId = vm.SubjectId,
                Title = vm.Title.Trim()
            };

            _db.SubjectChapters.Add(chapter);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateChapterAsync(ChapterEditVM vm)
        {
            var chapter = await _db.SubjectChapters.SingleOrDefaultAsync(ch => ch.Id == vm.ChapterId);
            if (chapter == null) throw new InvalidOperationException("Không tìm thấy chương.");

            chapter.Title = vm.Title.Trim();
            _db.SubjectChapters.Update(chapter);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteChapterAsync(int chapterId)
        {
            var chapter = await _db.SubjectChapters
                .Include(ch => ch.Clos)
                .SingleOrDefaultAsync(ch => ch.Id == chapterId);
            if (chapter == null) throw new InvalidOperationException("Không tìm thấy chương.");

            var chapterIds = new List<int> { chapterId };
            var cloIds = chapter.Clos.Select(c => c.Id).ToList();
            await EnsureChapterOrCloNotUsedAsync(chapterIds, cloIds);

            _db.SubjectChapters.Remove(chapter);
            await _db.SaveChangesAsync();
        }

        public async Task<List<SubjectClo>> GetClosByChapterAsync(int chapterId)
        {
            return await _db.SubjectClos
                .AsNoTracking()
                .Where(c => c.ChapterId == chapterId)
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        public async Task CreateCloAsync(CloCreateVM vm)
        {
            var chapterExists = await _db.SubjectChapters.AnyAsync(ch => ch.Id == vm.ChapterId);
            if (!chapterExists) throw new InvalidOperationException("Không tìm thấy chương.");

            var clo = new SubjectClo
            {
                ChapterId = vm.ChapterId,
                Title = vm.Title.Trim(),
                Description = vm.Description?.Trim()
            };

            _db.SubjectClos.Add(clo);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateCloAsync(CloEditVM vm)
        {
            var clo = await _db.SubjectClos.SingleOrDefaultAsync(c => c.Id == vm.CloId);
            if (clo == null) throw new InvalidOperationException("Không tìm thấy CLO.");

            clo.Title = vm.Title.Trim();
            clo.Description = vm.Description?.Trim();
            _db.SubjectClos.Update(clo);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteCloAsync(int cloId)
        {
            var clo = await _db.SubjectClos.SingleOrDefaultAsync(c => c.Id == cloId);
            if (clo == null) throw new InvalidOperationException("Không tìm thấy CLO.");

            await EnsureCloNotUsedAsync(cloId);

            _db.SubjectClos.Remove(clo);
            await _db.SaveChangesAsync();
        }

        private async Task EnsureChapterOrCloNotUsedAsync(List<int> chapterIds, List<int> cloIds)
        {
            if (chapterIds.Count == 0 && cloIds.Count == 0) return;

            bool usedByQuestionBanks = await _db.QuestionBanks.AnyAsync(q =>
                (q.ChapterId.HasValue && chapterIds.Contains(q.ChapterId.Value)) ||
                (q.CLOId.HasValue && cloIds.Contains(q.CLOId.Value)));

            bool usedByExamMatrices = await _db.ExamMatrices.AnyAsync(m =>
                chapterIds.Contains(m.ChapterId) || cloIds.Contains(m.CLOId));

            if (usedByQuestionBanks || usedByExamMatrices)
            {
                throw new InvalidOperationException("Không thể xóa do Chapter/CLO đang được sử dụng trong ngân hàng đề hoặc ma trận đề.");
            }
        }

        private async Task EnsureCloNotUsedAsync(int cloId)
        {
            bool usedByQuestionBanks = await _db.QuestionBanks.AnyAsync(q => q.CLOId == cloId);
            bool usedByExamMatrices = await _db.ExamMatrices.AnyAsync(m => m.CLOId == cloId);

            if (usedByQuestionBanks || usedByExamMatrices)
            {
                throw new InvalidOperationException("Không thể xóa CLO do CLO đang được sử dụng trong ngân hàng đề hoặc ma trận đề.");
            }
        }
    }
}

