using ITExam.Models;
using ITExam.Services;
using ITExam.ViewModels;
using ITExam.Filters;
using ITExam.ViewModels.Class;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace ITExam.Controllers
{
    public class AuthController : Controller
    {
        private readonly ITExamDbContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthController(ITExamDbContext context, IOptions<JwtSettings> jwtOptions)
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
        }

        [HttpGet]
        [CheckToken(redirectToLoginSuccessOnValid: true, redirectToLogoutOnInvalid: false)]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || !PasswordHasherUtil.VerifyPassword(model.Password, user.PasswordHash ?? string.Empty))
            {
                ModelState.AddModelError("", "Thông tin đăng nhập không đúng");
                return View(model);
            }

            // Create internal JWT (HttpOnly cookie).
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_jwtSettings.AccessTokenExpiresMinutes);

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("username", user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            user.AccessToken = jwt;
            user.UpdatedDate = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("access_token", jwt, new CookieOptions
            {
                Expires = expires,
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });

            TempData["SuccessMessage"] = "Đăng nhập thành công";
            return user.Role == "teacher"
                ? RedirectToAction("LoginSuccessTeacher")
                : user.Role == "admin"
                    ? RedirectToAction("Index", "Admin")
                    : RedirectToAction("LoginSuccessStudent");

        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
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

            var nextId = await _context.Users.MaxAsync(u => (int?)u.UserId) ?? 0;
            nextId += 1;

            var newUser = new User
            {
                UserId = nextId,
                Username = model.Username,
                FullName = model.FullName,
                Email = null,
                ClassName = null,
                Role = "user",
                Faculty = null,
                PasswordHash = PasswordHasherUtil.HashPassword(model.Password),
                AccessToken = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [AuthorizeRole("teacher")]
        public async Task<IActionResult> LoginSuccessTeacher()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Logout");
            }

            var user = await _context.Users.SingleOrDefaultAsync(us => us.UserId == userId);
            if (user == null) return RedirectToAction("Logout");

            var classes = _context.Classes.Where(l => l.UserId == user.UserId).ToList();

            var exams = new List<ClassExamVM>();
            foreach (var classItem in classes)
            {
                var deThiLopHoc = _context.ClassExams
                    .Where(d => d.ClassId == classItem.ClassId)
                    .Select(d => new ClassExamVM
                    {
                        ExamId = d.ExamId,
                        ExamName = d.Exam.ExamName,
                        AddedDate = d.AddedDate,
                        StartTime = d.StartTime,
                        EndTime = d.EndTime,
                        Duration = d.Exam.Duration,
                        ExamType = d.Exam.ExamType,
                        QuestionCount = d.Exam.ExamMatrices.Count,
                        ClassId = d.ClassId,
                        ClassName = classItem.ClassName
                    }).ToList();

                exams.AddRange(deThiLopHoc);
            }
            var viewModel = new UserInfoVM
            {
                UserId = user.UserId,
                HoTen = user.FullName,
                Email = user.Email,
                Lop = user.ClassName,
                DanhSachDeThi = exams
            };
            return View(viewModel);
        }
        [AuthorizeRole("user")]
        public async Task<IActionResult> LoginSuccessStudent()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Logout");
            }

            var user = await _context.Users.SingleOrDefaultAsync(us => us.UserId == userId);
            if (user == null) return RedirectToAction("Logout");
            var classIds = _context.ClassDetails
                .Where(c => c.UserId == user.UserId)
                .Select(c => c.ClassId)
                .ToList();
            var classes = _context.Classes
                .Where(l => classIds.Contains(l.ClassId))
                .ToList();
            var exams = new List<ClassExamVM>();
            foreach (var classItem in classes)
            {
                var deThiLopHoc = _context.ClassExams
                    .Where(d => d.ClassId == classItem.ClassId)
                    .Select(d => new ClassExamVM
                    {
                        ExamId = d.ExamId,
                        ExamName = d.Exam.ExamName,
                        AddedDate = d.AddedDate,
                        StartTime = d.StartTime,
                        EndTime = d.EndTime,
                        Duration = d.Exam.Duration,
                        ExamType = d.Exam.ExamType,
                        QuestionCount = d.Exam.ExamMatrices.Count,
                        ClassId = d.ClassId,
                        ClassName = classItem.ClassName,
                        IsDone = _context.ExamHistories
                            .Any(h => h.ExamId == d.ExamId && h.UserId == user.UserId)
                    })
                    .ToList();

                exams.AddRange(deThiLopHoc);
            }
            var viewModel = new UserInfoVM
            {
                UserId = user.UserId,
                HoTen = user.FullName,
                Email = user.Email,
                Lop = user.ClassName,
                Khoa = user.Faculty,
                DanhSachDeThi = exams
            };
            return View(viewModel);
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return RedirectToAction("Login");
        }

        public IActionResult Author()
        {
            return View();
        }
    }
}
