using ITExam.Filters;
using ITExam.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ITExam.Controllers
{
    [CheckToken]
    [AuthorizeRole("admin")]
    public class AdminController : Controller
    {
        private readonly ITExamDbContext _context;

        public AdminController(ITExamDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Logout", "Auth");
            }

            var admin = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (admin == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            ViewData["AdminFullName"] = admin.FullName;
            ViewData["TeacherCount"] = await _context.Users.CountAsync(u => u.Role == "teacher");
            ViewData["StudentCount"] = await _context.Users.CountAsync(u => u.Role == "user");
            ViewData["SubjectCount"] = await _context.Subjects.CountAsync();
            return View();
        }
    }
}

