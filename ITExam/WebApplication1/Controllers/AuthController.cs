using Microsoft.AspNetCore.Mvc;
using ITExam.ExternalModels.Auth;
using ITExam.ViewModels;
using ITExam.ExternalModels.Auth.ITExam.ExternalModels.Auth;
using System.Text.Json;
using ITExam.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ITExam.Filters;
using ITExam.ViewModels.Class;
using ITExam.Services;


namespace ITExam.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITExamDbContext _context;
        public AuthController(IHttpClientFactory httpClientFactory, ITExamDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid) return View(model);
            var client = _httpClientFactory.CreateClient();
            var formData = new Dictionary<string, string>
            {
                { "username", model.Username },
                { "password", model.Password },
                { "platform_auth", "itexam" }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, RoutingAPI.Login)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Thông tin đăng nhập không đúng");
                return View(model);
            }

            var content = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiLoginResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var userInfo = apiResponse.data.user_info;
            var token = apiResponse.data.token.access_token;

            var user = _context.Users.SingleOrDefault(u => u.Username == userInfo.username);

            if (user == null)
            {
                var newUser = new User
                {
                    UserId = userInfo.id,
                    Username = userInfo.username,
                    FullName = userInfo.nickname,
                    Email = userInfo.email,
                    ClassName = userInfo.class_name,
                    Role = userInfo.role,
                    Faculty = userInfo.faculty_name,
                    AccessToken = token,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                user = newUser;
            }
            else
            {
                user.AccessToken = token;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            Response.Cookies.Append("access_token", apiResponse.data.token.access_token, new CookieOptions { Expires = DateTimeOffset.Now.AddHours(4) });

            TempData["SuccessMessage"] = "Đăng nhập thành công";
            if (user.Role == "teacher")
            {
                return RedirectToAction("LoginSuccessTeacher");
            }
            return RedirectToAction("LoginSuccessStudent");

        }

        [AuthorizeRole("teacher")]
        public async Task<IActionResult> LoginSuccessTeacher()
        {
            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();

            if (string.IsNullOrEmpty(access_token))
            {
                return RedirectToAction("Logout");
            }

            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout");
            }
            var formData = new Dictionary<string, string>
            {
                { "access_token", access_token },
                { "user_id", user.UserId.ToString() },
                { "platform_auth", "itexam" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, RoutingAPI.CheckTokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };

            var response = await client.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseContent);
            if (doc.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean() == false)
            {
                return RedirectToAction("Logout");
            }

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
                        Type = d.IsExam,
                        CreatedDate = d.AddedDate,
                        StartDate = d.StartTime,
                        EndDate = d.EndTime,
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
            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();

            if (string.IsNullOrEmpty(access_token))
            {
                return RedirectToAction("Logout");
            }

            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout");
            }
            var formData = new Dictionary<string, string>
            {
                { "access_token", access_token },
                { "user_id", user.UserId.ToString() },
                { "platform_auth", "itexam" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, RoutingAPI.CheckTokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };

            var response = await client.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseContent);
            if (doc.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean() == false)
            {
                return RedirectToAction("Logout");
            }
            // Lấy danh sách lớp học mà sinh viên tham gia
            var classIds = _context.ClassDetails
                .Where(c => c.UserId == user.UserId)
                .Select(c => c.ClassId)
                .ToList();

            // Lấy danh sách lớp học
            var classes = _context.Classes
                .Where(l => classIds.Contains(l.ClassId))
                .ToList();

            // Lấy danh sách đề thi của các lớp học đó
            var exams = new List<ClassExamVM>();
            foreach (var classItem in classes)
            {
                var deThiLopHoc = _context.ClassExams
                    .Where(d => d.ClassId == classItem.ClassId)
                    .Select(d => new ClassExamVM
                    {
                        ExamId = d.ExamId,
                        ExamName = d.Exam.ExamName,
                        Type = d.IsExam,
                        CreatedDate = d.AddedDate,
                        StartDate = d.StartTime,
                        EndDate = d.EndTime,
                        Duration = d.Exam.Duration,
                        ExamType = d.Exam.ExamType,
                        QuestionCount = d.Exam.ExamMatrices.Count,
                        ClassId = d.ClassId,
                        ClassName = classItem.ClassName,

                        // 🔹 Check xem sinh viên đã làm đề này chưa
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
