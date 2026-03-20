using ITExam.Models;
using Microsoft.EntityFrameworkCore;

namespace ITExam.Services
{
    public static class AuthSampleDataSeeder
    {
        public static async Task SeedIfEmptyAsync(ITExamDbContext db)
        {
            var adminUsername = "adminTest";
            var adminPassword = "adminTest1@";
            var teacherUsername = "teacherTest";
            var teacherPassword = "teacherTest1@";
            var studentUsername = "studentTest";
            var studentPassword = "studentTest1@";

            var nextId = (await db.Users.MaxAsync(u => (int?)u.UserId) ?? 0) + 1;

            var admin = await db.Users.SingleOrDefaultAsync(u => u.Username == adminUsername);
            if (admin == null)
            {
                admin = new User
                {
                    UserId = nextId++,
                    Username = adminUsername,
                    FullName = "Admin Test",
                    Email = null,
                    ClassName = null,
                    Role = "admin",
                    Faculty = null,
                    PasswordHash = PasswordHasherUtil.HashPassword(adminPassword),
                    AccessToken = Guid.NewGuid().ToString("N"),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                db.Users.Add(admin);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(admin.PasswordHash))
                {
                    admin.PasswordHash = PasswordHasherUtil.HashPassword(adminPassword);
                }
                admin.Role = "admin";
                admin.UpdatedDate = DateTime.Now;
                if (string.IsNullOrWhiteSpace(admin.AccessToken))
                {
                    admin.AccessToken = Guid.NewGuid().ToString("N");
                }
            }

            var teacher = await db.Users.SingleOrDefaultAsync(u => u.Username == teacherUsername);
            if (teacher == null)
            {
                teacher = new User
                {
                    UserId = nextId++,
                    Username = teacherUsername,
                    FullName = "Teacher Test",
                    Email = null,
                    ClassName = null,
                    Role = "teacher",
                    Faculty = "Công ngh? thông tin",
                    PasswordHash = PasswordHasherUtil.HashPassword(teacherPassword),
                    AccessToken = Guid.NewGuid().ToString("N"),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                db.Users.Add(teacher);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(teacher.PasswordHash))
                {
                    teacher.PasswordHash = PasswordHasherUtil.HashPassword(teacherPassword);
                }
                teacher.Role = "teacher";
                teacher.UpdatedDate = DateTime.Now;
                if (string.IsNullOrWhiteSpace(teacher.AccessToken))
                {
                    teacher.AccessToken = Guid.NewGuid().ToString("N");
                }
            }

            var student = await db.Users.SingleOrDefaultAsync(u => u.Username == studentUsername);
            if (student == null)
            {
                student = new User
                {
                    UserId = nextId++,
                    Username = studentUsername,
                    FullName = "Student Test",
                    Email = null,
                    ClassName = null,
                    Role = "user",
                    Faculty = null,
                    PasswordHash = PasswordHasherUtil.HashPassword(studentPassword),
                    AccessToken = Guid.NewGuid().ToString("N"),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                db.Users.Add(student);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(student.PasswordHash))
                {
                    student.PasswordHash = PasswordHasherUtil.HashPassword(studentPassword);
                }
                student.Role = "user";
                student.UpdatedDate = DateTime.Now;
                if (string.IsNullOrWhiteSpace(student.AccessToken))
                {
                    student.AccessToken = Guid.NewGuid().ToString("N");
                }
            }

            await db.SaveChangesAsync();
        }
    }
}

