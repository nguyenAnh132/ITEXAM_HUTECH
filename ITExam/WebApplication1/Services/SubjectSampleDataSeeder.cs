using ITExam.Models;
using Microsoft.EntityFrameworkCore;

namespace ITExam.Services
{
    public static class SubjectSampleDataSeeder
    {
        public static async Task SeedIfEmptyAsync(ITExamDbContext db)
        {
            if (await db.Subjects.AnyAsync())
            {
                return; 
            }

            var math = new Subject
            {
                Code = "MTH101",
                Name = "Toan co ban",
            };

            var mathCh1 = new SubjectChapter
            {
                Title = "Chuong 1: Dai so"
            };
            mathCh1.Clos.Add(new SubjectClo { Title = "CLO 1", Description = "Dai so - CLO 1" });
            mathCh1.Clos.Add(new SubjectClo { Title = "CLO 2", Description = "Dai so - CLO 2" });

            var mathCh2 = new SubjectChapter
            {
                Title = "Chuong 2: Giai tich"
            };
            mathCh2.Clos.Add(new SubjectClo { Title = "CLO 1", Description = "Giai tich - CLO 1" });
            mathCh2.Clos.Add(new SubjectClo { Title = "CLO 2", Description = "Giai tich - CLO 2" });

            math.Chapters.Add(mathCh1);
            math.Chapters.Add(mathCh2);

            var prog = new Subject
            {
                Code = "PRG101",
                Name = "Lap trinh co ban",
            };

            var progCh1 = new SubjectChapter
            {
                Title = "Chuong 1: Co so lap trinh"
            };
            progCh1.Clos.Add(new SubjectClo { Title = "CLO 1", Description = "Co so - CLO 1" });
            progCh1.Clos.Add(new SubjectClo { Title = "CLO 2", Description = "Co so - CLO 2" });

            prog.Chapters.Add(progCh1);

            db.Subjects.Add(math);
            db.Subjects.Add(prog);

            await db.SaveChangesAsync();
        }
    }
}

