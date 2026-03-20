using ITExam.Filters;
using ITExam.Models;
using ITExam.Services;
using ITExam.ViewModels.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITExam.Controllers
{
    [CheckToken]
    [AuthorizeRole("admin")]
    public class AdminTeachersController : Controller
    {
        private readonly ITExamDbContext _context;

        public AdminTeachersController(ITExamDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var teachers = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "teacher")
                .OrderBy(u => u.UserId)
                .ToListAsync();

            return View(teachers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TeacherCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeacherCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _context.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ModelState.AddModelError("", "Tên tài khoản đã tồn tại.");
                return View(model);
            }

            var nextId = (await _context.Users.MaxAsync(u => (int?)u.UserId) ?? 0) + 1;
            var newUser = new User
            {
                UserId = nextId,
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                Faculty = model.Faculty,
                ClassName = null,
                Role = "teacher",
                PasswordHash = PasswordHasherUtil.HashPassword(model.Password),
                AccessToken = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm giảng viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int userId)
        {
            var user = await _context.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserId == userId && u.Role == "teacher");

            if (user == null) return NotFound();

            var vm = new TeacherEditVM
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Faculty = user.Faculty
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeacherEditVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == model.UserId && u.Role == "teacher");
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Faculty = model.Faculty;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.PasswordHash = PasswordHasherUtil.HashPassword(model.NewPassword);
            }

            user.UpdatedDate = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật giảng viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int userId)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId && u.Role == "teacher");
            if (user == null) return NotFound();
            bool hasClasses = await _context.Classes.AnyAsync(c => c.UserId == userId);
            bool hasExamBanks = await _context.ExamBanks.AnyAsync(eb => eb.UserId == userId);
            bool hasExams = await _context.Exams.AnyAsync(e => e.UserId == userId);
            bool hasLogs = await _context.ActivityLogs.AnyAsync(al => al.UserId == userId);

            if (hasClasses || hasExamBanks || hasExams || hasLogs)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa giảng viên do đang được dùng trong dữ liệu lớp/đề/ngân hàng đề/nhật ký.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa giảng viên thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}

