using ITExam.ExternalModels.Subject;
using ITExam.Models;
using Microsoft.EntityFrameworkCore;

namespace ITExam.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ITExamDbContext _db;

        public SubjectService(ITExamDbContext db)
        {
            _db = db;
        }

        public async Task<List<SubjectDto>> GetSubjectsAsync()
        {
            return await _db.Subjects
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Chapters = new List<ChapterDto>()
                })
                .ToListAsync();
        }

        public async Task<SubjectDto?> GetSubjectByIdAsync(int subjectId)
        {
            return await _db.Subjects
                .AsNoTracking()
                .Where(s => s.Id == subjectId)
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Chapters = s.Chapters.Select(ch => new ChapterDto
                    {
                        Id = ch.Id,
                        Title = ch.Title,
                        Clos = ch.Clos.Select(cl => new CloDto
                        {
                            Id = cl.Id,
                            Title = cl.Title,
                            Description = cl.Description
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}

