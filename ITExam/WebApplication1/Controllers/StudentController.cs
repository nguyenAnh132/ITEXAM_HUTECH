using ITExam.Filters;
using ITExam.Hubs;
using ITExam.Models;
using ITExam.ViewModels;
using ITExam.ViewModels.Class;
using ITExam.ViewModels.Exam;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebApplication1.Models;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Microsoft.AspNetCore.Authorization;
namespace ITExam.Controllers
{
    //[CheckToken]
    [AuthorizeRole("user")]
    public class StudentController : Controller
    {

        private readonly ITExamDbContext _context;
        private readonly IDistributedCache _redis;
        private readonly IHubContext<ExamMonitorHub> _hubContext;
        private readonly ILogger<StudentController> _logger;


        public StudentController(ITExamDbContext context, IDistributedCache redis, IHubContext<ExamMonitorHub> hubContext, ILogger<StudentController> logger   )
        {
            _context = context;
            _redis = redis;
            _hubContext = hubContext;
            _logger=logger;
        }
        public IActionResult Index()
        {
            return View();
        }


        // Lớp học: Danh sách lớp học -
        public async Task<IActionResult> Class()
        {
            //check_user
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var lopHoc = await _context.ClassDetails
                .Where(cd => cd.UserId == user.UserId)
                .Include(cd => cd.Class)
                .ToListAsync();

            var result = lopHoc.Select(cl => new ClassVM
            {
                ClassId = cl.Class.ClassId,
                ClassName = cl.Class.ClassName,
                CreatedDate = cl.Class.CreatedDate,
                StudentCount = _context.ClassDetails.Count(c => c.ClassId == cl.Class.ClassId),
                ExamCount = _context.ClassExams.Count(dt => dt.ClassId == cl.Class.ClassId)
            }).ToList();

            return View(result);
        }



        // Lớp học: Tham gia lớp học
        [HttpPost]
        public async Task<IActionResult> JoinClass([FromBody] JoinClassRequestVM req)
        {
            //Check tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            //Truy vấn lớp học
            var lopHoc = await _context.Classes.FirstOrDefaultAsync(cl => cl.ClassCode == req.Code);
            if (lopHoc == null)
            {
                return BadRequest(new { message = "Lớp học không tồn tại." });
            }

            var isAlreadyJoined = await _context.ClassDetails.AnyAsync(ct => ct.UserId == user.UserId && ct.ClassId == lopHoc.ClassId);
            if (isAlreadyJoined)
            {
                return BadRequest(new { message = "Bạn đã tham gia lớp học này rồi!" });
            }

            ClassDetail newClassDetail = new ClassDetail
            {
                UserId = user.UserId,
                JoinDate = DateTime.Now,
                ClassId = lopHoc.ClassId
            };
            try
            {
                _context.ClassDetails.Add(newClassDetail);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Tham gia lớp học thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tham gia lớp học!", error = ex.Message });
            }
        }

        // Lớp học: Thoát lớp học đã tham gia.
        [HttpPost]
        public async Task<IActionResult> ExitClass(int malophoc)
        {
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);  

            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var chiTiet = await _context.ClassDetails
                .FirstOrDefaultAsync(c => c.UserId == user.UserId && c.ClassId == malophoc);

            if (chiTiet == null)
            {
                TempData["Message"] = "Không tìm thấy lớp học để thoát.";
                return RedirectToAction("Class");
            }

            _context.ClassDetails.Remove(chiTiet);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Bạn đã thoát khỏi lớp học thành công.";
            return RedirectToAction("Class");
        }


        // Lớp học: Kiểm tra lớp học có tồn tại hay không -
        [HttpGet]
        public async Task<JsonResult> CheckClass(string code)
        {
            //Tuy vấn lớp học
            var lopHoc = await _context.Classes.FirstOrDefaultAsync(l => l.ClassCode.ToString() == code);

            if (lopHoc == null)
            {
                return Json(new { exists = false, message = "Lớp học không tồn tại...." });
            }  

            return Json(new { exists = true, className = lopHoc.ClassName });
        }


        //Lớp học : Chi tiết lớp học -
        public async Task<IActionResult> ClassDetail(int id)
        {
            //Check tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            //Truy vấn lớp học và bao gồm các thông tin liên quan: chi tiết lớp học,danh sách sinh viên, danh sách đề thi, lịch sử làm bài
            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(cl => cl.ClassId == id)
                .Select(cl => new StudentClassInfoVM
                {
                    ClassId = cl.ClassId,
                    ClassName = cl.ClassName,
                    Description = cl.Description,
                    InstructorName = cl.User.FullName
                })
                .FirstOrDefaultAsync();

            if (classInfo is null)
            {
                return NotFound();
            }
            
            var studentListOfClass = await _context.ClassDetails
                .AsNoTracking()
                .Where(cd => cd.ClassId == id)
                .Include(cd => cd.User)
                .Select(cd => new ClassStudentVM
                {
                    StudentId = cd.User.Username,
                    FullName = cd.User.FullName
                })
                .ToListAsync();

            var examsInClass = await _context.ClassExams
                .AsNoTracking()
                .Where(ce => ce.ClassId == id)
                .Select(ce => new StudentClassExamVM
                {
                    ExamId = ce.Exam.ExamId,
                    ExamName = ce.Exam.ExamName,
                    StartTime = ce.StartTime,
                    EndTime = ce.EndTime,
                    Duration = ce.Exam.Duration,
                    ExamType = ce.Exam.ExamType,
                    SubmitTime = ce.Exam.ExamHistories
                    .FirstOrDefault(ls => ls.UserId == user.UserId && ls.ExamId == ce.Exam.ExamId && ls.ClassId == id).SubmitTime,
                    QuestionCount = ce.Exam.ExamMatrices.Sum(sl => sl.QuestionCount)
                })
                .ToListAsync();

            var viewModel = new StudentClassDetailVM
            {
                ClassInfo = classInfo,
                Students = studentListOfClass,
                Exams = examsInClass,
                StudentCount = examsInClass.Count(),
                ExamCount = examsInClass.Count()
            };

            return View(viewModel);
        }



        // Đề thi: Thông tin đề thi của sinh viên.
        public async Task<IActionResult> ExamInfo(int maDe, int maLop)
        {
            var deThi = await _context.Exams
                .Include(mt => mt.ExamMatrices)
                .Include(us => us.User)
                .FirstOrDefaultAsync(dt => dt.ExamId == maDe);

            if (deThi == null)
                return NotFound();

            var viewModel = new ExamVM
            {
                ExamId = deThi.ExamId,
                ExamName = deThi.ExamName,
                FullName = deThi.User.FullName,
                CreatedDate = deThi.CreatedDate,
                ExamType = deThi.ExamType,
                QuestionCount = deThi.ExamMatrices.Sum(sl => sl.QuestionCount),
                Duration = deThi.Duration,
                ClassId = maLop
            };
            return View(viewModel);
        }



        // Đề thi : Kiểm tra thời gian vào thi
        [HttpGet]
        public JsonResult KiemTraThoiGianVaoThi(int maDe)
        {
            var deThi = _context.ClassExams.FirstOrDefault(x => x.ExamId == maDe);

            if (deThi == null)
                return Json(new { success = false, message = "Không tìm thấy đề." });

            var now = DateTime.Now;

            if (now < deThi.StartTime)
                return Json(new { success = false, message = "Chưa đến thời gian bắt đầu." });

            if (now > deThi.EndTime)
                return Json(new { success = false, message = "Đề thi đã hết hạn." });

            return Json(new { success = true });
        }


        // Đề thi: Vào thi
        public async Task<IActionResult> TakeTheExam_TN(int maDe, int maLop)
        {
            //check_user
            var access_token = Request.Cookies["access_token"];
            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            //Kiểm tra xem sinh viên đã thi chưa
            var deThi = await _context.Exams.FindAsync(maDe);
            if (deThi == null) return NotFound();

            var lichSu = await _context.ExamHistories
                .FirstOrDefaultAsync(l => l.UserId == user.UserId && l.ExamId == maDe && l.ClassId == maLop);

            if (lichSu != null && lichSu.SubmitTime.HasValue)
            {
                TempData["Error"] = "Bạn đã nộp bài này rồi!";
                return RedirectToAction("Class");
            }

            var chiTietBaiLamList = new List<StudentAnswer>();

            if (lichSu == null)
            {
                lichSu = new ExamHistory
                {
                    UserId = user.UserId,
                    ExamId = maDe,
                    ClassId = maLop,
                    StartTime = DateTime.Now,
                    Score = 0
                };
                _context.ExamHistories.Add(lichSu);
                await _context.SaveChangesAsync();

                //Random đề thi cho sinh viên
                var maTran = await _context.ExamMatrices.Where(m => m.ExamId == maDe).ToListAsync();
                var random = new Random();

                var cauHoiNHD = await _context.QuestionBanks.Where(ch => ch.ExamBankId == deThi.ExamBankId).ToListAsync();

                foreach (var item in maTran)
                {
                    var cauHoiTheoCLO = cauHoiNHD
                        .Where(c => c.ChapterId == item.ChapterId && c.CLOId == item.CLOId)
                        .ToList();

                    var selected = cauHoiTheoCLO
                        .OrderBy(x => random.Next())
                        .Take(item.QuestionCount)
                        .ToList();

                    foreach (var ch in selected)
                    {
                        var chiTiet = new StudentAnswer
                        {
                            ExamHistoryId = lichSu.ExamHistoryId,
                            QuestionId = ch.QuestionId
                        };
                        _context.StudentAnswers.Add(chiTiet);
                        chiTietBaiLamList.Add(chiTiet);
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                chiTietBaiLamList = await _context.StudentAnswers
                    .Where(ct => ct.ExamHistoryId == lichSu.ExamHistoryId)
                    .ToListAsync();
            }

            var danhSachCauHoi = _context.QuestionBanks
                .Where(ch => chiTietBaiLamList.Select(c => c.QuestionId).Contains(ch.QuestionId))
                .ToList();

            var lopHoc = await _context.Classes.FindAsync(maLop);

            // Load câu trả lời trắc nghiệm từ Redis
            var key = $"multiChoiceAns:{maDe}:{user.UserId}";
            var cacheData = await _redis.GetStringAsync(key);
            var luuLuaChon = string.IsNullOrEmpty(cacheData)
                ? new Dictionary<int, List<int>>()
                : JsonSerializer.Deserialize<Dictionary<int, List<int>>>(cacheData);

            // Load câu trả lời tự luận từ Redis
            var essayKey = $"essayAns:{maDe}:{user.UserId}";
            var essayCacheData = await _redis.GetStringAsync(essayKey);
            var luuCauTraLoiTuLuan = string.IsNullOrEmpty(essayCacheData)
                ? new Dictionary<int, string>()
                : JsonSerializer.Deserialize<Dictionary<int, string>>(essayCacheData);

            int thoiGianLamBai = deThi.Duration * 60;
            int thoiGianDaTroiQua = (int)(DateTime.Now - lichSu.StartTime).TotalSeconds;
            int thoiGianConLai = thoiGianLamBai - thoiGianDaTroiQua;
            if (thoiGianConLai < 0) thoiGianConLai = 0;

            var vm = new MultipleChoiceExamEntryVM
            {
                ExamId = deThi.ExamId,
                ExamName = deThi.ExamName,
                ExamType = deThi.ExamType,
                Duration = thoiGianConLai,
                UserId = user.UserId,
                StudentId = user.Username,
                FullName = user.FullName,
                Questions = danhSachCauHoi.Select(ch => new MultipleChoiceQuestionVM
                {
                    QuestionId = ch.QuestionId,
                    QuestionContent = ch.QuestionContent,
                    QuestionType = ch.QuestionType,
                    // Load lại giá trị cho textarea nếu là câu hỏi loại 4 (điền từ)
                    FillInAns = ch.QuestionType == "4" && luuCauTraLoiTuLuan.ContainsKey(ch.QuestionId)
                        ? luuCauTraLoiTuLuan[ch.QuestionId]
                        : string.Empty,
                    Choices = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionOptionVM>>(ch.ChoiceContent)
                        .Select(lc => new ChoiceVM
                        {
                            ChoiceId = lc.ChoiceId,
                            ChoiceContent = lc.ChoiceContent,
                            IsSelected = luuLuaChon.ContainsKey(ch.QuestionId) && luuLuaChon[ch.QuestionId].Contains(lc.ChoiceId)
                        }).ToList()
                }).ToList(),
                ClassId = maLop,
                ClassName = lopHoc.ClassName
            };

            return View(vm);
        }

        //Đề thi : Lưu redis tự luận
        [HttpPost]
        public async Task<IActionResult> SaveEssayAnswer([FromBody] LuuTuLuan data)
        {
            //check_user
            var access_token = Request.Cookies["access_token"];
            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            string key = $"essayAns:{data.MaDe}:{user.UserId}";

            var json = JsonSerializer.Serialize(data.CauTraLoi);
            await _redis.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(300)
            });

            return Ok(new { success = true });
        }



        //Đề thi : Vào thi tự luận
        public async Task<IActionResult> TakeTheExam_TL(int maDe, int maLop) // Bỏ IFormFile ở đây
        {
            //Kiểm tra tài khoản của sinh viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            //Truy vấn đề thi
            var deThi = await _context.Exams.FindAsync(maDe);
            if (deThi == null) return NotFound();

            //Truy vấn lịch sử làm bài của sinh viên trong lớp đó
            var lichSu = await _context.ExamHistories
                .FirstOrDefaultAsync(l => l.UserId == user.UserId && l.ExamId == maDe && l.ClassId == maLop);

            //Nếu đã có lịch sử làm bài và đã nộp bài thì không cho vào thi nữa
            if (lichSu != null && lichSu.SubmitTime.HasValue)
            {
                TempData["Error"] = "Bạn đã nộp bài này rồi!";
                return RedirectToAction("Class");
            }

            //Nếu chưa có lịch sử làm bài thì tạo mới
            var chiTietBaiLamList = new List<StudentAnswer>();
            if (lichSu == null)
            {
                lichSu = new ExamHistory
                {
                    UserId = user.UserId,
                    ExamId = maDe,
                    ClassId = maLop,
                    StartTime = DateTime.Now,
                };
                _context.ExamHistories.Add(lichSu);
                await _context.SaveChangesAsync();

                //Random đề thi cho sinh viên
                var maTran = await _context.ExamMatrices.Where(m => m.ExamId == maDe).ToListAsync();
                var random = new Random();

                //Truy vấn danh sách câu hỏi theo mã ngân hàng đề
                var cauHoiNHD = await _context.QuestionBanks.Where(ch => ch.ExamBankId == deThi.ExamBankId).ToListAsync();

                foreach (var item in maTran)
                {
                    var cauHoiTheoCLO = cauHoiNHD
                        .Where(c => c.ChapterId == item.ChapterId && c.CLOId == item.CLOId)
                        .ToList();

                    var selected = cauHoiTheoCLO
                        .OrderBy(x => random.Next())
                        .Take(item.QuestionCount)
                        .ToList();

                    foreach (var ch in selected)
                    {
                        var chiTiet = new StudentAnswer
                        {
                            ExamHistoryId = lichSu.ExamHistoryId,
                            QuestionId = ch.QuestionId
                        };
                        _context.StudentAnswers.Add(chiTiet);
                        chiTietBaiLamList.Add(chiTiet);
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                chiTietBaiLamList = await _context.StudentAnswers
                    .Where(ct => ct.ExamHistoryId == lichSu.ExamHistoryId)
                    .ToListAsync();
            }

            var danhSachCauHoi = _context.QuestionBanks
                .Where(ch => chiTietBaiLamList.Select(c => c.QuestionId).Contains(ch.QuestionId))
                .ToList();

            var lopHoc = await _context.Classes.FindAsync(maLop);

            // === SỬA LỖI ĐỌC REDIS VÀ THÊM LOG ===
            var key = $"essayAns:{maDe}:{user.UserId}";
            Dictionary<string, string> luuCauTraLoi = null;
            try
            {
                var cacheData = await _redis.GetStringAsync(key);

                // SỬA LỖI: PHẢI LÀ <string, string>
                luuCauTraLoi = string.IsNullOrEmpty(cacheData)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(cacheData);

                if (luuCauTraLoi.Any())
                    _logger.LogInformation(">>> REDIS (TakeTheExam_TL): Đọc thành công {Count} câu trả lời từ key {Key}", luuCauTraLoi.Count, key);
                else
                    _logger.LogWarning(">>> REDIS (TakeTheExam_TL): Key {Key} rỗng hoặc không tồn tại.", key);
            }
            catch (Exception ex)
            {
                // LỖI NÀY CÓ THỂ XẢY RA NẾU TRƯỚC ĐÓ BẠN LƯU BẰNG <int, string>
                _logger.LogError(ex, ">>> LỖI (TakeTheExam_TL): Deserialize Redis key <string, string> THẤT BẠI. Key {Key}", key);
                luuCauTraLoi = new Dictionary<string, string>(); // Khởi tạo rỗng để tránh crash
            }
            // === KẾT THÚC SỬA LỖI ===

            int thoiGianLamBai = deThi.Duration * 60;
            int thoiGianDaTroiQua = (int)(DateTime.Now - lichSu.StartTime).TotalSeconds;
            int thoiGianConLai = thoiGianLamBai - thoiGianDaTroiQua;
            if (thoiGianConLai < 0) thoiGianConLai = 0;

            var viewModel = new EssayExamStartVM
            {
                ExamId = deThi.ExamId,
                ExamName = deThi.ExamName,
                ExamType = deThi.ExamType,
                Duration = thoiGianConLai,
                UserId = user.UserId,
                StudentId = user.Username,
                FullName = user.FullName,
                Questions = danhSachCauHoi.Select(ch =>
                {
                    // SỬA LỖI: PHẢI DÙNG .ToString()
                    string questionIdKey = ch.QuestionId.ToString();
                    return new EssayQuestionVM
                    {
                        QuestionId = ch.QuestionId,
                        QuestionContent = ch.QuestionContent,
                        EssayAnswer = luuCauTraLoi.ContainsKey(questionIdKey)
                                        ? luuCauTraLoi[questionIdKey]
                                        : null
                    };
                }).ToList(),
                ClassId = maLop,
                ClassName = lopHoc.ClassName
            };

            return View(viewModel);
        }


        //Đề thi : Lưu quá trình làm bài của sinh viên vào Redis 
        [HttpPost]
        public async Task<IActionResult> SaveAnswer([FromBody] LuuLuaChon request)
        {
            var key = $"multiChoiceAns:{request.MaDe}:{request.UserId}";
            var cacheData = await _redis.GetStringAsync(key);

            var answer = string.IsNullOrEmpty(cacheData)
                ? new Dictionary<int, List<int>>()
                : JsonSerializer.Deserialize<Dictionary<int, List<int>>>(cacheData);

            if (request.IsMultiChoice)
            {
                if (!answer.ContainsKey(request.MaCauHoi))
                {
                    answer[request.MaCauHoi] = new List<int>();
                }

                if (answer[request.MaCauHoi].Contains(request.MaLuaChon))
                {
                    answer[request.MaCauHoi].Remove(request.MaLuaChon);
                }
                else
                {
                    answer[request.MaCauHoi].Add(request.MaLuaChon);
                }
            }
            else
            {
                if (answer.ContainsKey(request.MaCauHoi) &&
                    answer[request.MaCauHoi].Count == 1 &&
                    answer[request.MaCauHoi][0] == request.MaLuaChon)
                {
                    answer.Remove(request.MaCauHoi);
                }
                else
                {
                    answer[request.MaCauHoi] = new List<int> { request.MaLuaChon };
                }
            }

            var json = JsonSerializer.Serialize(answer);

            await _redis.SetStringAsync(key, json, options: new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(305)
            });

            return Ok(new { success = true });
        }

        // Loại bỏ HTML tags, decode entities
        private static string StripHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // decode entities like &nbsp;
            string decoded = WebUtility.HtmlDecode(input);
            // remove tags like <p> ... </p>
            string noTags = Regex.Replace(decoded, "<.*?>", string.Empty);
            return noTags;
        }

        private static string RemoveInvisibleAndCollapseSpaces(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string s = input.Replace("\u00A0", " ")   // non-breaking space
                            .Replace("\u200B", "");   // zero-width space
            s = Regex.Replace(s, @"\s+", " ").Trim();   // collapse multiple spaces
            return s;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }


        /// Normalize chuẩn để so sánh: strip html, decode, remove invisible, remove punctuation (but keep spaces),
        /// tùy chọn có bỏ dấu không, sau đó ToUpper với ngôn ngữ vi-VN.
        private static string NormalizeForComparison(string input, bool removeDiacritics = false)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string s = StripHtml(input);
            s = RemoveInvisibleAndCollapseSpaces(s);

            // Loại bỏ punctuation (.,;:!?()[]{}<>'"...) nhưng giữ khoảng trắng
            s = new string(s.Where(c => !char.IsPunctuation(c)).ToArray());

            if (removeDiacritics)
                s = RemoveDiacritics(s);

            // Chuyển hết về uppercase theo culture Việt Nam (an toàn với tiếng Việt)
            s = s.ToUpper(new CultureInfo("vi-VN"));

            return s;
        }


        // Đề thi : Kết quả thi trắc nghiệm của sinh viên
        [HttpPost]
        public async Task<IActionResult> ExamResult_TN(int maDe, int maLop)
        {
            //check_user
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.SingleOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var deThi = await _context.Exams.FindAsync(maDe);
            if (deThi == null) return NotFound();

            var examHistory = await _context.ExamHistories
                .FirstOrDefaultAsync(ls => ls.UserId == user.UserId && ls.ExamId == maDe);
            if (examHistory == null) return NotFound();

            // Lấy dữ liệu câu trả lời trắc nghiệm
            var multiChoiceKey = $"multiChoiceAns:{maDe}:{user.UserId}";
            var multiChoiceCacheData = await _redis.GetStringAsync(multiChoiceKey);

            // Lấy dữ liệu câu trả lời điền từ
            var essayKey = $"essayAns:{maDe}:{user.UserId}";
            var essayCacheData = await _redis.GetStringAsync(essayKey);

            var tongCauHoi = await _context.ExamMatrices
                .Where(mt => mt.ExamId == maDe)
                .SumAsync(mt => mt.QuestionCount);

            double diemMoiCau = 10.0 / tongCauHoi;
            double tongDiem = 0;
            int soCauDung = 0;

            // Xử lý câu trả lời trắc nghiệm
            if (!string.IsNullOrEmpty(multiChoiceCacheData))
            {
                var multiChoiceAnsList = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(multiChoiceCacheData);

                if (multiChoiceAnsList != null && multiChoiceAnsList.Count > 0)
                {
                    foreach (var cauTraLoi in multiChoiceAnsList)
                    {
                        int maCauHoi = cauTraLoi.Key;
                        List<int> luaChonSV = cauTraLoi.Value.OrderBy(x => x).ToList();

                        var cauHoi = await _context.QuestionBanks
                            .SingleOrDefaultAsync(ch => ch.QuestionId == maCauHoi);
                        if (cauHoi == null) continue;

                        // Chỉ xử lý câu trắc nghiệm (loại 1, 2, 3)
                        if (cauHoi.QuestionType == "1" || cauHoi.QuestionType == "2" || cauHoi.QuestionType == "3")
                        {
                            var dapAnDung = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionOptionVM>>(cauHoi.ChoiceContent)
                                .Where(lc => lc.IsCorrectAnswer)
                                .Select(lc => lc.ChoiceId)
                                .OrderBy(x => x)
                                .ToList();

                            bool isCorrect = luaChonSV.SequenceEqual(dapAnDung);
                            if (isCorrect)
                            {
                                tongDiem += diemMoiCau;
                                soCauDung++;
                            }
                        }
                    }
                }
            }

            // Xử lý câu trả lời điền từ
            if (!string.IsNullOrEmpty(essayCacheData))
            {
                var fillInAnsList = JsonSerializer.Deserialize<Dictionary<int, string>>(essayCacheData);

                if (fillInAnsList != null && fillInAnsList.Count > 0)
                {
                    foreach (var cauTraLoi in fillInAnsList)
                    {
                        int maCauHoi = cauTraLoi.Key;
                        string cauTraLoiSV = cauTraLoi.Value ?? "";
                        if (string.IsNullOrWhiteSpace(cauTraLoiSV)) continue;

                        var cauHoi = await _context.QuestionBanks
                            .SingleOrDefaultAsync(ch => ch.QuestionId == maCauHoi);
                        if (cauHoi == null) continue;

                        if (cauHoi.QuestionType == "4")
                        {
                            // Lấy đáp án đúng từ ChoiceContent (có thể chứa HTML)
                            var dapAnDungList = Newtonsoft.Json.JsonConvert
                                .DeserializeObject<List<QuestionOptionVM>>(cauHoi.ChoiceContent)
                                .Where(lc => lc.IsCorrectAnswer)
                                .Select(lc => lc.ChoiceContent ?? "")
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => NormalizeForComparison(x, removeDiacritics: false)) // true nếu muốn so sánh bỏ dấu (Hà nội = Ha noi)
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToList();

                            // Chuẩn hóa câu trả lời SV
                            string cauTraLoiSVNorm = NormalizeForComparison(cauTraLoiSV, removeDiacritics: false);

                            // So sánh chính xác giữa các string đã normalize
                            bool isCorrect = dapAnDungList.Any(dapAn => string.Equals(dapAn, cauTraLoiSVNorm, StringComparison.Ordinal));

                            if (isCorrect)
                            {
                                tongDiem += diemMoiCau;
                                soCauDung++;
                            }
                        }
                    }
                }
            }


            var viewModel = new MultipleChoiceExamResultVM
            {
                Score = Math.Round(tongDiem, 2),
                QuestionCount = tongCauHoi,
                CorrectAnswers = soCauDung,
                AccuracyRate = tongCauHoi > 0 ? Math.Round((double)soCauDung * 100 / tongCauHoi, 2) : 0,
                ExamDate = DateTime.Now,
                Duration = (int)(DateTime.Now - examHistory.StartTime).TotalSeconds,
                ClassId = maLop
            };

            // Lưu lịch sử làm bài
            examHistory.Score = viewModel.Score;
            examHistory.SubmitTime = DateTime.Now;
            examHistory.Duration = viewModel.Duration;
            _context.ExamHistories.Update(examHistory);

            // Lưu chi tiết làm bài - Trắc nghiệm
            if (!string.IsNullOrEmpty(multiChoiceCacheData))
            {
                var multiChoiceAnsList = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(multiChoiceCacheData);
                if (multiChoiceAnsList != null)
                {
                    foreach (var ch in multiChoiceAnsList)
                    {
                        var cauHoi = ch.Key;
                        var luaChon = ch.Value;

                        var chiTietBaiLam = await _context.StudentAnswers
                            .FirstOrDefaultAsync(ct => ct.ExamHistoryId == examHistory.ExamHistoryId && ct.QuestionId == cauHoi);
                        if (chiTietBaiLam != null)
                        {
                            chiTietBaiLam.MultipleChoiceAnswer = string.Join(",", luaChon);
                            _context.StudentAnswers.Update(chiTietBaiLam);
                        }
                    }
                }
            }

            // Lưu chi tiết làm bài - Điền từ
            if (!string.IsNullOrEmpty(essayCacheData))
            {
                var fillInAnsList = JsonSerializer.Deserialize<Dictionary<int, string>>(essayCacheData);
                if (fillInAnsList != null)
                {
                    foreach (var ch in fillInAnsList)
                    {
                        var cauHoi = ch.Key;
                        var cauTraLoi = ch.Value;

                        var chiTietBaiLam = await _context.StudentAnswers
                            .FirstOrDefaultAsync(ct => ct.ExamHistoryId == examHistory.ExamHistoryId && ct.QuestionId == cauHoi);
                        if (chiTietBaiLam != null)
                        {
                            chiTietBaiLam.EssayAnswer = cauTraLoi; 
                            _context.StudentAnswers.Update(chiTietBaiLam);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Xóa Redis
            await _redis.RemoveAsync(multiChoiceKey);
            await _redis.RemoveAsync(essayKey);

            await _hubContext.Clients
                    .Group($"exam:{maDe}:class:{maLop}")
                    .SendAsync("StudentScoreUpdated", new
                    {
                        ExamId = maDe.ToString(),
                        ClassId = maLop.ToString(),
                        StudentId = user.Username,
                        StudentName = user.FullName ?? "",
                        Score = viewModel.Score,
                        CorrectAnswers = viewModel.CorrectAnswers,
                        QuestionCount = viewModel.QuestionCount,
                        AccuracyRate = viewModel.AccuracyRate,
                        Duration = viewModel.Duration,
                        SubmittedAt = DateTime.UtcNow
                    });

            return View(viewModel);
        }


        public async Task<IActionResult> ExamResult_TL(int maDe, int maLop)
        {
            //check_user
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.SingleOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var examHistory = await _context.ExamHistories
                .Include(eh => eh.Exam)
                .FirstOrDefaultAsync(ls => ls.UserId == user.UserId && ls.ExamId == maDe && ls.ClassId == maLop);

            if (examHistory == null)
            {
                TempData["Error"] = "Không có lịch sử làm bài cho đề thi này.";
                return RedirectToAction("ClassDetail", new { id = maLop });
            }

            var studentAnswers = await _context.StudentAnswers
                .Where(ct => ct.ExamHistoryId == examHistory.ExamHistoryId)
                .Include(ct => ct.QuestionBank)
                .ToListAsync();

            // TODO: build ViewModel để show kết quả, điểm, câu trả lời,...

            ViewBag.MaLop = maLop;
            return View(/* viewModel */);
        }


        [HttpPost]
        public async Task<IActionResult> ExamResult_TL([FromBody] EssaySubmitVM model) // Giữ nguyên model của bạn
        {
            // 1. Lấy thông tin User
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return Unauthorized(new { message = "Phiên đăng nhập hết hạn" }); // Trả về JSON
            }

            // 2. Tìm lịch sử làm bài
            var lichSu = await _context.ExamHistories
                .FirstOrDefaultAsync(l => l.UserId == user.UserId && l.ExamId == model.MaDe && l.ClassId == model.MaLop);

            if (lichSu == null)
            {
                return NotFound(new { message = "Không tìm thấy lịch sử làm bài." }); // Trả về JSON
            }

            // 3. Chỉ cập nhật nếu bài chưa nộp
            if (!lichSu.SubmitTime.HasValue)
            {
                lichSu.SubmitTime = DateTime.Now;
                lichSu.Duration = (int)(DateTime.Now - lichSu.StartTime).TotalSeconds; // Tính thời gian
                lichSu.UploadedFileId = model.UploadedFileId; // Lưu File ID

                // 4. LẤY LOGIC TỪ HÀM GET DÁN VÀO ĐÂY
                // Đọc câu trả lời cuối cùng từ Redis TRƯỚC KHI XÓA
                var key = $"essayAns:{model.MaDe}:{user.UserId}";
                var savedAnswers = new Dictionary<int, string>(); // <int, string>

                try
                {
                    var cacheData = await _redis.GetStringAsync(key);
                    if (!string.IsNullOrEmpty(cacheData))
                    {
                        // JS/Redis lưu là <string, string> (theo các script trước)
                        var redisAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(cacheData);

                        // Chuyển <string, string> sang <int, string> để lưu vào CSDL
                        foreach (var kvp in redisAnswers)
                        {
                            if (int.TryParse(kvp.Key, out int questionId))
                            {
                                savedAnswers[questionId] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi đọc hoặc parse Redis key {Key}", key);
                    // Tiếp tục với danh sách trả lời rỗng
                }

                // 5. Lưu câu trả lời (từ Redis) vào CSDL
                var studentAnswers = await _context.StudentAnswers
                    .Where(ct => ct.ExamHistoryId == lichSu.ExamHistoryId)
                    .ToListAsync();

                foreach (var answer in studentAnswers)
                {
                    if (savedAnswers.ContainsKey(answer.QuestionId))
                    {
                        answer.EssayAnswer = savedAnswers[answer.QuestionId];
                        _context.StudentAnswers.Update(answer);
                    }
                }

                // 6. Lưu tất cả thay đổi vào CSDL (Lịch sử + Câu trả lời)
                _context.ExamHistories.Update(lichSu);
                await _context.SaveChangesAsync(); // Chỉ cần 1 lần Save

                // 7. Xóa cache Redis (Sau khi đã lưu CSDL thành công)
                try
                {
                    await _redis.RemoveAsync(key);
                    _logger.LogInformation(">>> REDIS (ExamResult_TL POST): Đã xóa key {Key} sau khi nộp bài.", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, ">>> LỖI (ExamResult_TL POST): Không thể xóa key Redis {Key}.", key);
                }
            }

            // 8. Trả về JSON chứa link chuyển hướng
            // Link này trỏ đến hàm [GET] ExamResult_TL
            var redirectUrl = Url.Action("ExamResult_TL", "Student", new { maDe = model.MaDe, maLop = model.MaLop });
            return Ok(new { success = true, redirectUrl = redirectUrl });
        }
        public async Task<IActionResult> List_History_Detail()
        {
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(u => u.AccessToken == access_token);
            if (user == null)
                return RedirectToAction("Logout", "Auth");

            var historyList = await _context.ExamHistories
                .Where(ls => ls.UserId == user.UserId)
                .Include(ls => ls.Exam)
                    .ThenInclude(d => d.ClassExams)
                .Include(ls => ls.Class)
                .ToListAsync();

            var viewModelList = historyList.Select(ls => new StudentExamHistoryVM
            {
                ClassId = ls.ClassId,
                ExamId = ls.ExamId,
                ExamHistoryId = ls.ExamHistoryId,
                ClassName = ls.Class.ClassName,
                ExamName = ls.Exam.ExamName,
                StartTime = ls.StartTime,
                SubmissionTime = ls.SubmitTime,
                Score = ls.Score,
                IsExam = ls.Exam.ClassExams
                              .FirstOrDefault(d => d.ClassId == ls.ClassId)?.IsExam ?? true,
                Access = ls.Exam.ClassExams
                              .FirstOrDefault(d => d.ClassId == ls.ClassId)?.Access ?? true

            }).ToList();

            return View(viewModelList);
        }


        //Đề thi : Chi tiết lịch sử làm bài của sinh viên
        public async Task<IActionResult> Exam_History_Detail(int id)
        {
            // Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // Truy vấn và ánh xạ dữ liệu chi tiết lịch sử làm bài
            var examHistory = await _context.ExamHistories
                .Include(ls => ls.Exam)
                .Where(ls => ls.ExamHistoryId == id)
                .Select(ls => new ExamHistoryVM
                {
                    StartTime = ls.StartTime,
                    SubmitTime = ls.SubmitTime,
                    Score = ls.Score,
                    ClassId = ls.ClassId,
                    ExamName = ls.Exam.ExamName,
                    ExamType = ls.Exam.ExamType,
                    ExamId = ls.ExamId,
                    UploadedFileUrl = ls.UploadedFileId,
                    ExamDetails = ls.StudentAnswers.Select(ct => new ExamHistoryDetailVM
                    {
                        QuestionContent = ct.QuestionBank.QuestionContent,
                        ChoiceContent = ct.QuestionBank.ChoiceContent,
                        QuestionType = ct.QuestionBank.QuestionType,
                        MultipleChoiceAnswer = ct.MultipleChoiceAnswer,
                        EssayAnswer = ct.EssayAnswer,
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (examHistory == null)
            {
                return NotFound("Không tìm thấy lịch sử làm bài.");
            }

            return View(examHistory);
        }


        [HttpGet]
        public async Task<IActionResult> GetQuestions(int maDe)
        {
            var exam = await _context.Exams
                .Include(d => d.ExamMatrices)
                .FirstOrDefaultAsync(d => d.ExamId == maDe);

            if (exam == null)
                return NotFound(new { message = "Không tìm thấy đề thi." });

            var questionList = await _context.QuestionBanks
                .Where(c => c.ExamBankId == exam.ExamBankId)
                .ToListAsync();

            var result = questionList.Select((question, index) => new
            {
                id = index + 1,
                category = "HTML", // hoặc thay bằng question.ChapterName nếu bạn có
                question = question.QuestionContent,
                options = Newtonsoft.Json.JsonConvert
                             .DeserializeObject<List<QuestionOptionVM>>(question.ChoiceContent)
                             .Select(lc => lc.ChoiceContent)
                             .ToList()
                // Không thêm right_answer
            });

            return Json(new { questions = result });
        }
        [HttpPost]
        public async Task<IActionResult> UploadEssayFile(int maDe, int maLop, IFormFile file)
        {
            // 1. Kiểm tra tài khoản của sinh viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.SingleOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Chưa đăng nhập" });
            }

            // 2. Kiểm tra file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Không có file nào được chọn" });
            }

            // 3. Truy vấn lịch sử làm bài (PHẢI TỒN TẠI)
            var lichSu = await _context.ExamHistories
                .FirstOrDefaultAsync(ls => ls.UserId == user.UserId && ls.ExamId == maDe && ls.ClassId == maLop);

            if (lichSu == null)
            {
                // Lỗi này không nên xảy ra nếu sinh viên đang ở trang làm bài
                return NotFound(new { success = false, message = "Không tìm thấy lịch sử làm bài" });
            }

            // 4. Nếu sinh viên đã nộp bài thì không cho upload đè
            if (lichSu.SubmitTime.HasValue)
            {
                return BadRequest(new { success = false, message = "Bạn đã nộp bài, không thể upload lại" });
            }

            // 5. XỬ LÝ UPLOAD FILE LÊN GOOGLE DRIVE
            string fileId;
            try
            {
                // Giả sử bạn đã inject _driveService vào Controller
                //fileId = await _driveService.UploadFileAsync(file, user.FullName, maDe);
            }
            catch (System.Exception ex)
            {
                // Ghi log lỗi (ex.Message)
                _logger.LogError(ex, ">>> LỖI (UploadEssayFile) GOOGLE DRIVE UPLOAD");
                return StatusCode(500, new { success = false, message = "Lỗi khi tải file lên Google Drive." });
            }

            // 6. Cập nhật File ID vào CSDL
            //lichSu.UploadedFileId = fileId;
            _context.ExamHistories.Update(lichSu);
            await _context.SaveChangesAsync();

            // 7. Trả về kết quả thành công cho JavaScript
            return Ok(new
            {
                success = true,
                message = "Upload file thành công!",
                fileId = 0,
                fileName = file.FileName
            });
        }


    }
}



//Xong het controller => Chia ViewModel ra sua => Tiep tuc sua View => Toc Do, Logic cua action