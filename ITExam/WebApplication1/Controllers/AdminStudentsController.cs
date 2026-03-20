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
    public class AdminStudentsController : Controller
    {
        private readonly ITExamDbContext _context;

        public AdminStudentsController(ITExamDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var students = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "user")
                .OrderBy(u => u.UserId)
                .ToListAsync();

            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(new StudentCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentCreateVM model)
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
                Role = "user",
                PasswordHash = PasswordHasherUtil.HashPassword(model.Password),
                AccessToken = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm sinh viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int userId)
        {
            var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId && u.Role == "user");
            if (user == null) return NotFound();

            var vm = new StudentEditVM
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
        public async Task<IActionResult> Edit(StudentEditVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == model.UserId && u.Role == "user");
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Faculty = model.Faculty;

            // Password is optional: only hash if provided.
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                user.PasswordHash = PasswordHasherUtil.HashPassword(model.NewPassword);
            }

            user.UpdatedDate = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật sinh viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int userId)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == userId && u.Role == "user");
            if (user == null) return NotFound();

            // Chặn xóa nếu đang được dùng trong dữ liệu quan trọng.
            bool hasClassJoin = await _context.ClassDetails.AnyAsync(cd => cd.UserId == userId);
            bool hasExamHistory = await _context.ExamHistories.AnyAsync(eh => eh.UserId == userId);

            if (hasClassJoin || hasExamHistory)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa sinh viên do đã tham gia lớp và/hoặc có lịch sử làm bài.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa sinh viên thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu reset mật khẩu không hợp lệ.";
                return RedirectToAction(nameof(Edit), new { userId = model.UserId });
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserId == model.UserId && u.Role == "user");
            if (user == null) return NotFound();

            user.PasswordHash = PasswordHasherUtil.HashPassword(model.NewPassword);
            user.UpdatedDate = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reset mật khẩu thành công!";
            return RedirectToAction(nameof(Edit), new { userId = model.UserId });
        }
    }
}

