using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace ITExam.Models
{
    public class ITExamDbContext : DbContext
    {
        public ITExamDbContext(DbContextOptions<ITExamDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassDetail> ClassDetails { get; set; }
        public DbSet<ExamBank> ExamBanks { get; set; }
        public DbSet<QuestionBank> QuestionBanks { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ClassExam> ClassExams { get; set; }
        public DbSet<ExamHistory> ExamHistories { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<ExamMatrix> ExamMatrices { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite keys
            modelBuilder.Entity<ClassDetail>()
                .HasKey(cd => new { cd.UserId, cd.ClassId });

            modelBuilder.Entity<ClassDetail>()
                .HasOne(cd => cd.Class)
                .WithMany(c => c.ClassDetails)
                .HasForeignKey(cd => cd.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassExam>()
                .HasKey(ce => new { ce.ClassId, ce.ExamId });

            modelBuilder.Entity<ClassExam>()
                .HasOne(ce => ce.Class)
                .WithMany(c => c.ClassExams)
                .HasForeignKey(ce => ce.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // User relationships - NO CASCADE
            modelBuilder.Entity<ClassDetail>()
                .HasOne(cd => cd.User)
                .WithMany(u => u.ClassDetails)
                .HasForeignKey(cd => cd.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.User)
                .WithMany(u => u.Classes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExamBank>()
                .HasOne(eb => eb.User)
                .WithMany(u => u.ExamBanks)
                .HasForeignKey(eb => eb.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExamHistory>()
                .HasOne(eh => eh.User)
                .WithMany(u => u.ExamHistories)
                .HasForeignKey(eh => eh.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.ExamBank)
                .WithMany(eb => eb.Exams)
                .HasForeignKey(e => e.ExamBankId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<QuestionBank>()
                .HasOne(qb => qb.ExamBank)
                .WithMany(eb => eb.QuestionBanks)
                .HasForeignKey(qb => qb.ExamBankId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.QuestionBank)
                .WithMany(qb => qb.StudentAnswers)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.ExamHistory)
                .WithMany(eh => eh.StudentAnswers)
                .HasForeignKey(sa => sa.ExamHistoryId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExamMatrix>()
                .HasOne(em => em.Exam)
                .WithMany(e => e.ExamMatrices)
                .HasForeignKey(em => em.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamHistory>()
                .HasOne(eh => eh.Exam)
                .WithMany(e => e.ExamHistories)
                .HasForeignKey(eh => eh.ExamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ActivityLog>()
                .HasOne(al => al.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
