using Google.Apis.Drive.v3.Data;
using Hangfire;
using ITExam.Filters;
using ITExam.Hubs;
using ITExam.Models;
using ITExam.ViewModels;
using ITExam.ViewModels.Class;
using ITExam.ViewModels.Exam;
using MathNet.Numerics.Distributions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using NPOI.HPSF;
using Org.BouncyCastle.Utilities.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ITExam.Services;

namespace ITExam.Controllers
{
    [CheckToken]
    [AuthorizeRole("user")]
    public class StudentController : Controller
    {

        private readonly ITExamDbContext _context;
        private readonly IDistributedCache _redis;
        private readonly IRedisService _redisService;
        private readonly IHubContext<ExamMonitorHub> _hubContext;
        private readonly ILogger<StudentController> _logger;
        private readonly IMemoryCache _cache;
        private readonly GeminiService geminiService;


        public StudentController(ITExamDbContext context, IDistributedCache redis, IRedisService redisService, IHubContext<ExamMonitorHub> hubContext, ILogger<StudentController> logger, IMemoryCache cache, GeminiService geminiService)
        {
            _context = context;
            _redis = redis;
            _hubContext = hubContext;
            _logger = logger;
            _cache = cache;
            _redisService = redisService;
            this.geminiService = geminiService;
        }
        public IActionResult Index()
        {
            return View();
        }


        // Lớp học: Danh sách lớp học -
        public async Task<IActionResult> Class()
        {
            //check_user
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var @class = await _context.ClassDetails
                .AsNoTracking()
                .Where(cd => cd.UserId == userId)
                .Select(cd => new ClassVM
                {
                    ClassId = cd.Class.ClassId,
                    ClassName = cd.Class.ClassName,
                    CreatedDate = cd.Class.CreatedDate,
                    StudentCount = cd.Class.ClassDetails.Count(),
                    ExamCount = cd.Class.ClassExams.Count(),
                    LecturerName = cd.Class.User.FullName
                })
                .ToListAsync();

            return View(@class);
        }



        // Lớp học: Tham gia lớp học
        [HttpPost]
        public async Task<IActionResult> JoinClass([FromBody] JoinClassRequestVM req)
        {
            // 1. Check tài khoản
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            if (userId == 0)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(c => c.ClassCode == req.Code)
                .Select(c => new
                {
                    c.ClassId,
                    IsAlreadyJoined = c.ClassDetails.Any(cd => cd.UserId == userId)
                })
                .FirstOrDefaultAsync();

            if (classInfo == null)
            {
                return BadRequest(new { message = "Lớp học không tồn tại." });
            }

            if (classInfo.IsAlreadyJoined)
            {
                return BadRequest(new { message = "Bạn đã tham gia lớp học này rồi!" });
            }

            ClassDetail newClassDetail = new ClassDetail
            {
                UserId = userId,
                JoinDate = DateTime.Now,
                ClassId = classInfo.ClassId
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
            // 1. Check tài khoản 
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            if (userId == 0)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var chiTietToDelete = new ClassDetail
            {
                UserId = userId,
                ClassId = malophoc
            };

            _context.ClassDetails.Remove(chiTietToDelete);

            try
            {
                var rowsAffected = await _context.SaveChangesAsync();

                if (rowsAffected > 0)
                {
                    TempData["SuccessMessage"] = "Bạn đã thoát khỏi lớp học thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy lớp học để thoát.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cố gắng thoát lớp.";
            }

            return RedirectToAction("Class");
        }


        // Lớp học: Kiểm tra lớp học có tồn tại hay không -
        [HttpGet]
        public async Task<JsonResult> CheckClass(string code)
        {
            //Tuy vấn lớp học
            var lopHoc = await _context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ClassCode.ToString() == code);

            if (lopHoc == null)
            {
                return Json(new { exists = false, message = "Lớp học không tồn tại...." });
            }

            return Json(new { exists = true, className = lopHoc.ClassName });
        }


        //Lớp học : Chi tiết lớp học -
        public async Task<IActionResult> ClassDetail(int id)
        {
            // 1. Check tài khoản 
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            if (userId == 0)
            {
                return RedirectToAction("Logout", "Auth");
            }

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
                    .FirstOrDefault(ls => ls.UserId == userId && ls.ExamId == ce.Exam.ExamId && ls.ClassId == id).SubmitTime,
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
            var exam = await _context.Exams
                .AsNoTracking()
                .Include(mt => mt.ExamMatrices)
                .Include(us => us.User)
                .FirstOrDefaultAsync(dt => dt.ExamId == maDe);

            if (exam == null)
                return NotFound();

            var viewModel = new ExamVM
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                FullName = exam.User.FullName,
                CreatedDate = exam.CreatedDate,
                ExamType = exam.ExamType,
                QuestionCount = exam.ExamMatrices.Sum(sl => sl.QuestionCount),
                Duration = exam.Duration,
                ClassId = maLop
            };
            return View(viewModel);
        }



        // Đề thi : Kiểm tra thời gian vào thi
        [HttpGet]
        public JsonResult KiemTraThoiGianVaoThi(int maDe)
        {
            var deThi = _context.ClassExams.AsNoTracking().FirstOrDefault(x => x.ExamId == maDe);

            if (deThi == null)
                return Json(new { success = false, message = "Không tìm thấy đề." });

            var now = DateTime.Now;

            if (now < deThi.StartTime)
                return Json(new { success = false, message = "Chưa đến thời gian bắt đầu." });

            if (now > deThi.EndTime)
                return Json(new { success = false, message = "Đề thi đã hết hạn." });

            return Json(new { success = true });
        }


        public async Task AutoSubmitExam(int examHistoryId)
        {
            var examHistory = await _context.ExamHistories
                .FirstOrDefaultAsync(h => h.ExamHistoryId == examHistoryId);
            if (examHistory == null)
                return;
            if (examHistory.SubmitTime.HasValue)
                return;
            if (examHistory.EndTime != default && DateTime.Now < examHistory.EndTime)
                return;

            var user = await _context.Users
                .AsNoTracking()
                .Where(us => us.UserId == examHistory!.UserId)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Username
                })
                .FirstOrDefaultAsync();
            if (user == null)
                return;

            int examId = examHistory.ExamId;
            int classId = examHistory.ClassId;

            // Lấy dữ liệu câu trả lời trắc nghiệm
            var multiChoiceKey = $"multiChoiceAns:{examId}:{user.UserId}";
            var multiChoiceCacheData = await _redis.GetStringAsync(multiChoiceKey);

            // Lấy dữ liệu câu trả lời điền từ
            var essayKey = $"essayAns:{examId}:{user.UserId}";
            var essayCacheData = await _redis.GetStringAsync(essayKey);

            var questionCount = await _context.ExamMatrices
                .AsNoTracking()
                .Where(mt => mt.ExamId == examId)
                .SumAsync(mt => mt.QuestionCount);

            if (questionCount <= 0)
            {
                examHistory.Score = 0;
                examHistory.SubmitTime = DateTime.Now;

                if (!string.IsNullOrEmpty(examHistory.HangfireJobId))
                {
                    BackgroundJob.Delete(examHistory.HangfireJobId);
                    examHistory.HangfireJobId = null;
                }

                await _context.SaveChangesAsync();
                return;
            }

            double scoreOfQuestion = 10.0 / questionCount;
            double totalScore = 0;
            int CorrectCount = 0;

            // Xử lý câu trả lời trắc nghiệm
            if (!string.IsNullOrEmpty(multiChoiceCacheData))
            {
                var multiChoiceAnsList = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(multiChoiceCacheData);

                if (multiChoiceAnsList != null && multiChoiceAnsList.Count > 0)
                {
                    foreach (var answer in multiChoiceAnsList)
                    {
                        int questionId = answer.Key;
                        List<int> answers = answer.Value.OrderBy(x => x).ToList();

                        var question = await _context.QuestionBanks
                            .SingleOrDefaultAsync(ch => ch.QuestionId == questionId);
                        if (question == null) continue;

                        // Chỉ xử lý câu trắc nghiệm (loại 1, 2, 3)
                        if (question.QuestionType == "1" || question.QuestionType == "2" || question.QuestionType == "3")
                        {
                            var correctAnswers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionOptionVM>>(question.ChoiceContent!)!
                                .Where(lc => lc.IsCorrectAnswer)
                                .Select(lc => lc.ChoiceId)
                                .OrderBy(x => x)
                                .ToList();

                            bool isCorrect = answers.SequenceEqual(correctAnswers);
                            if (isCorrect)
                            {
                                totalScore += scoreOfQuestion;
                                CorrectCount++;
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
                    foreach (var answer in fillInAnsList)
                    {
                        int questionId = answer.Key;
                        string cauTraLoiSV = answer.Value ?? "";
                        if (string.IsNullOrWhiteSpace(cauTraLoiSV)) continue;

                        var question = await _context.QuestionBanks
                            .AsNoTracking()
                            .SingleOrDefaultAsync(ch => ch.QuestionId == questionId);
                        if (question == null) continue;

                        if (question.QuestionType == "4")
                        {
                            // Lấy đáp án đúng từ ChoiceContent (có thể chứa HTML)
                            var correctAnswers = Newtonsoft.Json.JsonConvert
                                .DeserializeObject<List<QuestionOptionVM>>(question.ChoiceContent!)!
                                .Where(lc => lc.IsCorrectAnswer)
                                .Select(lc => lc.ChoiceContent ?? "")
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => NormalizeForComparison(x, removeDiacritics: false)) // true nếu muốn so sánh bỏ dấu (Hà nội = Ha noi)
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToList();

                            // Chuẩn hóa câu trả lời SV
                            string cauTraLoiSVNorm = NormalizeForComparison(cauTraLoiSV, removeDiacritics: false);

                            // So sánh chính xác giữa các string đã normalize
                            bool isCorrect = correctAnswers.Any(dapAn => string.Equals(dapAn, cauTraLoiSVNorm, StringComparison.Ordinal));

                            if (isCorrect)
                            {
                                totalScore += scoreOfQuestion;
                                CorrectCount++;
                            }
                        }
                    }
                }
            }

            // Lưu lịch sử làm bài
            examHistory.Score = Math.Round(totalScore, 2);
            examHistory.SubmitTime = DateTime.Now;
            examHistory.Duration = (int)(examHistory.EndTime - examHistory.StartTime).TotalSeconds;
            _context.ExamHistories.Update(examHistory);

            // Lưu chi tiết làm bài - Trắc nghiệm
            if (!string.IsNullOrEmpty(multiChoiceCacheData))
            {
                var multiChoiceAnsList = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(multiChoiceCacheData);
                if (multiChoiceAnsList != null)
                {
                    foreach (var ch in multiChoiceAnsList)
                    {
                        var question = ch.Key;
                        var option = ch.Value;

                        var detail = await _context.StudentAnswers
                            .FirstOrDefaultAsync(ct => ct.ExamHistoryId == examHistory.ExamHistoryId && ct.QuestionId == question);
                        if (detail != null)
                        {
                            detail.MultipleChoiceAnswer = string.Join(",", option);
                            _context.StudentAnswers.Update(detail);
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
                        var question = ch.Key;
                        var answer = ch.Value;

                        var detail = await _context.StudentAnswers
                            .FirstOrDefaultAsync(ct => ct.ExamHistoryId == examHistory.ExamHistoryId && ct.QuestionId == question);
                        if (detail != null)
                        {
                            detail.EssayAnswer = answer;
                            _context.StudentAnswers.Update(detail);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(examHistory.HangfireJobId))
            {
                BackgroundJob.Delete(examHistory.HangfireJobId);
                examHistory.HangfireJobId = null;
            }

            await _context.SaveChangesAsync();

            // Xóa Redis
            await _redis.RemoveAsync(multiChoiceKey);
            await _redis.RemoveAsync(essayKey);
            string orderKey = $"examOptionOrder:{examId}:{user.UserId}";
            await _redis.RemoveAsync(orderKey);

            try
            {
                var examKey = ExamKey(examId.ToString(), classId.ToString());
                var scoreMap = GetScores(examKey);
                scoreMap[user.Username] = Math.Round(totalScore, 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving score to IMemoryCache: {ex.Message}");
            }

            int tabLeaveCount = 0;
            var tabCountStr = await _redisService.GetStringAsync($"ITExam_tabcount:{examId}:{classId}:{user.Username}");
            Console.WriteLine($"ITExam_tabcount:{examId}:{classId}:{user.Username}-{tabCountStr}");
            if (!string.IsNullOrEmpty(tabCountStr) && int.TryParse(tabCountStr, out int parsedCount))
            {
                tabLeaveCount = parsedCount;
            }

            await _hubContext.Clients
            .Group($"exam:{examId}:class:{classId}")
            .SendAsync("StudentSubmittedExam", new
            {
                ExamId = examId.ToString(),
                StudentId = user.Username,
                StudentName = user.FullName ?? "",
                TabLeaveCount = tabLeaveCount, 
                SubmittedAt = DateTime.UtcNow,
                ExamHistoryId = examHistory.ExamHistoryId
            });

            await _redisService.AddLogAsync($"ITExam_logs:{examId}:{classId}:{user.Username}",
                $"[SUBMIT] [{DateTime.Now:HH:mm:ss}] {user.FullName} đã nộp bài thi. Số lần chuyển tab {tabLeaveCount}");

            await _hubContext.Clients
             .Group($"exam:{examId}:class:{classId}")
             .SendAsync("StudentScoreUpdated", new
             {
                 ExamId = examId.ToString(),
                 ClassId = classId.ToString(),
                 StudentId = user.Username,
                 StudentName = user.FullName ?? "",
                 Score = Math.Round(totalScore, 2),
                 Duration = (int)(DateTime.Now - examHistory.StartTime).TotalSeconds,
                 ExamHistoryId = examHistory.ExamHistoryId
             });
        }

        public async Task AutoSubmitEssayExam(int examHistoryId)
        {
            var examHistory = await _context.ExamHistories
                .FirstOrDefaultAsync(h => h.ExamHistoryId == examHistoryId);
            if (examHistory == null)
                return;
            if (examHistory.SubmitTime.HasValue)
                return;
            if (examHistory.EndTime != default && DateTime.Now < examHistory.EndTime)
                return;

            var user = await _context.Users
                .AsNoTracking()
                .Where(us => us.UserId == examHistory!.UserId)
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Username
                })
                .FirstOrDefaultAsync();
            if (user == null)
                return;

            int examId = examHistory.ExamId;
            int classId = examHistory.ClassId;

            if (!examHistory.SubmitTime.HasValue)
            {
                examHistory.SubmitTime = DateTime.Now;
                examHistory.Duration = (int)(examHistory.EndTime - examHistory.StartTime).TotalSeconds;

                var key = $"essayAns:{examId}:{user.UserId}";
                var savedAnswers = new Dictionary<int, string>();

                try
                {
                    var cacheData = await _redis.GetStringAsync(key);
                    if (!string.IsNullOrEmpty(cacheData))
                    {
                        var redisAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(cacheData);

                        foreach (var kvp in redisAnswers!)
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
                }

                var studentAnswers = await _context.StudentAnswers
                    .Where(ct => ct.ExamHistoryId == examHistory.ExamHistoryId)
                    .ToListAsync();

                foreach (var answer in studentAnswers)
                {
                    if (savedAnswers.ContainsKey(answer.QuestionId))
                    {
                        answer.EssayAnswer = savedAnswers[answer.QuestionId];
                        _context.StudentAnswers.Update(answer);
                    }
                }

                _context.ExamHistories.Update(examHistory);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(examHistory.HangfireJobId))
                {
                    BackgroundJob.Delete(examHistory.HangfireJobId);
                    examHistory.HangfireJobId = null;
                }

                await _context.SaveChangesAsync();

                string jobId = BackgroundJob.Enqueue(() =>
                    GadingEssayExamByAI(examHistory.ExamHistoryId, examHistory.UserId)
                );

                examHistory.HangfireJobId = jobId;
                await _context.SaveChangesAsync();

                try
                {
                    await _redis.RemoveAsync(key);
                    _logger.LogInformation("REDIS (ExamResult_TL POST): Đã xóa key {Key} sau khi nộp bài.", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LỖI (ExamResult_TL POST): Không thể xóa key Redis {Key}.", key);
                }
            }

        }


        // Đề thi: Vào thi
        public async Task<IActionResult> TakeTheExam_TN(int maDe, int maLop)
        {
            //check_user
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users
                .AsNoTracking()
                .Where(us => us.AccessToken == access_token)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FullName
                })
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            //Kiểm tra xem sinh viên đã thi chưa
            var exam = await _context.Exams.FindAsync(maDe);
            if (exam == null) return NotFound();

            var examHistory = await _context.ExamHistories
                .FirstOrDefaultAsync(l => l.UserId == user.UserId && l.ExamId == maDe && l.ClassId == maLop);

            if (examHistory != null && examHistory.SubmitTime.HasValue)
            {
                TempData["Error"] = "Bạn đã nộp bài này rồi!";
                return RedirectToAction("Class");
            }

            var details = new List<StudentAnswer>();

            if (examHistory == null)
            {
                examHistory = new ExamHistory
                {
                    UserId = user.UserId,
                    ExamId = maDe,
                    ClassId = maLop,
                    StartTime = DateTime.Now,
                    Score = 0
                };
                _context.ExamHistories.Add(examHistory);
                await _context.SaveChangesAsync();


                //plan
                examHistory.EndTime = examHistory.StartTime.AddMinutes(exam.Duration);
                var autoSubmitId = BackgroundJob.Schedule(
                    () => AutoSubmitExam(examHistory.ExamHistoryId),
                    TimeSpan.FromMinutes(exam.Duration)
                );

                examHistory.HangfireJobId = autoSubmitId;
                await _context.SaveChangesAsync();

                //Random đề thi cho sinh viên
                var maTran = await _context.ExamMatrices.AsNoTracking().Where(m => m.ExamId == maDe).ToListAsync();
                var random = new Random();

                var cauHoiNHD = await _context.QuestionBanks.AsNoTracking().Where(ch => ch.ExamBankId == exam.ExamBankId).ToListAsync();

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
                            ExamHistoryId = examHistory.ExamHistoryId,
                            QuestionId = ch.QuestionId
                        };
                        _context.StudentAnswers.Add(chiTiet);
                        details.Add(chiTiet);
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                details = await _context.StudentAnswers.AsNoTracking()
                    .Where(ct => ct.ExamHistoryId == examHistory.ExamHistoryId)
                    .ToListAsync();
            }

            var danhSachCauHoi = (from ct in details
                                  join ch in _context.QuestionBanks.AsNoTracking()
                                  on ct.QuestionId equals ch.QuestionId
                                  select ch)
                      .ToList();

            var lopHoc = await _context.Classes.FindAsync(maLop);

            // --- START: LOGIC ĐẢO THỨ TỰ ĐÁP ÁN ---

            // 1. Định nghĩa Key lưu thứ tự đáp án
            string orderKey = $"examOptionOrder:{maDe}:{user.UserId}";
            var orderCacheData = await _redis.GetStringAsync(orderKey);

            Dictionary<int, List<int>> shuffleMap; // Key: QuestionId, Value: List<ChoiceId> đã đảo

            if (string.IsNullOrEmpty(orderCacheData))
            {
                // Nếu chưa có (lần đầu vào thi): Tạo mới và Random
                shuffleMap = new Dictionary<int, List<int>>();
                var random = new Random();

                foreach (var ch in danhSachCauHoi)
                {
                    // Lấy list ChoiceId gốc
                    var choices = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionOptionVM>>(ch.ChoiceContent);
                    if (choices != null && choices.Any())
                    {
                        // Random thứ tự ID và lưu vào map
                        var shuffledIds = choices.Select(c => c.ChoiceId)
                                                 .OrderBy(x => random.Next())
                                                 .ToList();
                        shuffleMap[ch.QuestionId] = shuffledIds;
                    }
                }

                // Lưu vào Redis (Thời gian hết hạn = thời gian làm bài + buffer 30p)
                await _redis.SetStringAsync(orderKey, JsonSerializer.Serialize(shuffleMap), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(exam.Duration + 30)
                });
            }
            else
            {
                // Nếu đã có (F5 hoặc vào lại): Lấy ra dùng
                shuffleMap = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(orderCacheData)!;
            }
            // --- END: LOGIC ĐẢO THỨ TỰ ĐÁP ÁN ---

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

            int thoiGianLamBai = exam.Duration * 60;
            int thoiGianDaTroiQua = (int)(DateTime.Now - examHistory.StartTime).TotalSeconds;
            int thoiGianConLai = thoiGianLamBai - thoiGianDaTroiQua;
            if (thoiGianConLai < 0) thoiGianConLai = 0;

            var vm = new MultipleChoiceExamEntryVM
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                Duration = thoiGianConLai,
                UserId = user.UserId,
                StudentId = user.Username,
                FullName = user.FullName,
                Questions = danhSachCauHoi.Select(ch =>
                {
                    // Deserialize các lựa chọn gốc
                    var originalChoices = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionOptionVM>>(ch.ChoiceContent);

                    // Logic sắp xếp lại dựa trên shuffleMap
                    List<ChoiceVM> finalChoices;

                    if (shuffleMap.ContainsKey(ch.QuestionId))
                    {
                        var orderList = shuffleMap[ch.QuestionId]; // List ID đã random: [3, 1, 2]

                        // Join hoặc OrderBy để sắp xếp originalChoices theo thứ tự của orderList
                        finalChoices = originalChoices!
                            .OrderBy(c => orderList.IndexOf(c.ChoiceId))
                            .Select(lc => new ChoiceVM
                            {
                                ChoiceId = lc.ChoiceId,
                                ChoiceContent = lc.ChoiceContent!,
                                IsSelected = luuLuaChon!.ContainsKey(ch.QuestionId) && luuLuaChon[ch.QuestionId].Contains(lc.ChoiceId)
                            }).ToList();
                    }
                    else
                    {
                        // Fallback nếu không tìm thấy trong map (trường hợp hiếm)
                        finalChoices = originalChoices!.Select(lc => new ChoiceVM
                        {
                            ChoiceId = lc.ChoiceId,
                            ChoiceContent = lc.ChoiceContent!,
                            IsSelected = luuLuaChon!.ContainsKey(ch.QuestionId) && luuLuaChon[ch.QuestionId].Contains(lc.ChoiceId)
                        }).ToList();
                    }

                    return new MultipleChoiceQuestionVM
                    {
                        QuestionId = ch.QuestionId,
                        QuestionContent = ch.QuestionContent,
                        QuestionType = ch.QuestionType!,
                        FillInAns = ch.QuestionType == "4" && luuCauTraLoiTuLuan!.ContainsKey(ch.QuestionId)
                            ? luuCauTraLoiTuLuan[ch.QuestionId]
                            : string.Empty,
                        Choices = finalChoices
                    };
                }).ToList(),
                ClassId = maLop,
                ClassName = lopHoc!.ClassName
            };

            return View(vm);
        }

        //Đề thi : Lưu redis tự luận
        [HttpPost]
        public async Task<IActionResult> SaveEssayAnswer([FromBody] LuuTuLuan data)
        {
            //check_user
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            string key = $"essayAns:{data.MaDe}:{userId}";

            var json = JsonSerializer.Serialize(data.CauTraLoi);
            await _redis.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(300)
            });

            return Ok(new { success = true });
        }


        //Đề thi : Vào thi tự luận
        public async Task<IActionResult> TakeTheExam_TL(int maDe, int maLop)
        {
            //Kiểm tra tài khoản của sinh viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users
                .AsNoTracking()
                .Where(us => us.AccessToken == access_token)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FullName
                })
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var exam = await _context.Exams.FindAsync(maDe);
            if (exam == null) return NotFound();

            //Truy vấn lịch sử làm bài của sinh viên trong lớp đó
            var examHistory = await _context.ExamHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserId == user.UserId && l.ExamId == maDe && l.ClassId == maLop);

            //Nếu đã có lịch sử làm bài và đã nộp bài thì không cho vào thi nữa
            if (examHistory != null && examHistory.SubmitTime.HasValue)
            {
                TempData["Error"] = "Bạn đã nộp bài này rồi!";
                return RedirectToAction("Class");
            }

            //Nếu chưa có lịch sử làm bài thì tạo mới
            var details = new List<StudentAnswer>();
            if (examHistory == null)
            {
                examHistory = new ExamHistory
                {
                    UserId = user.UserId,
                    ExamId = maDe,
                    ClassId = maLop,
                    StartTime = DateTime.Now,
                };
                _context.ExamHistories.Add(examHistory);
                await _context.SaveChangesAsync();

                examHistory.EndTime = examHistory.StartTime.AddMinutes(exam.Duration);
                var autoSubmitId = BackgroundJob.Schedule(
                    () => AutoSubmitEssayExam(examHistory.ExamHistoryId),
                    TimeSpan.FromMinutes(exam.Duration)
                );

                examHistory.HangfireJobId = autoSubmitId;
                await _context.SaveChangesAsync();

                //Random đề thi cho sinh viên
                var maTran = await _context.ExamMatrices.AsNoTracking().Where(m => m.ExamId == maDe).ToListAsync();
                var random = new Random();

                //Truy vấn danh sách câu hỏi theo mã ngân hàng đề
                var cauHoiNHD = await _context.QuestionBanks.AsNoTracking().Where(ch => ch.ExamBankId == exam.ExamBankId).ToListAsync();

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
                            ExamHistoryId = examHistory.ExamHistoryId,
                            QuestionId = ch.QuestionId
                        };
                        _context.StudentAnswers.Add(chiTiet);
                        details.Add(chiTiet);
                    }
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                details = await _context.StudentAnswers
                    .Where(ct => ct.ExamHistoryId == examHistory.ExamHistoryId)
                    .ToListAsync();
            }

            var danhSachCauHoi = _context.QuestionBanks
                .AsNoTracking()
                .Where(ch => details.Select(c => c.QuestionId).Contains(ch.QuestionId))
                .ToList();

            var lopHoc = await _context.Classes.FindAsync(maLop);

            var key = $"essayAns:{maDe}:{user.UserId}";

            Dictionary<string, string> luuCauTraLoi = null!;

            try
            {
                var cacheData = await _redis.GetStringAsync(key);

                luuCauTraLoi = string.IsNullOrEmpty(cacheData)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(cacheData)!;

                if (luuCauTraLoi!.Any())
                    _logger.LogInformation("REDIS (TakeTheExam_TL): Đọc thành công {Count} câu trả lời từ key {Key}", luuCauTraLoi.Count, key);
                else
                    _logger.LogWarning("REDIS (TakeTheExam_TL): Key {Key} rỗng hoặc không tồn tại.", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI (TakeTheExam_TL): Deserialize Redis key <string, string> THẤT BẠI. Key {Key}", key);
                luuCauTraLoi = new Dictionary<string, string>();
            }

            int thoiGianLamBai = exam.Duration * 60;
            int thoiGianDaTroiQua = (int)(DateTime.Now - examHistory.StartTime).TotalSeconds;
            int thoiGianConLai = thoiGianLamBai - thoiGianDaTroiQua;
            if (thoiGianConLai < 0) thoiGianConLai = 0;

            var viewModel = new EssayExamStartVM
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                Duration = thoiGianConLai,
                UserId = user.UserId,
                StudentId = user.Username,
                FullName = user.FullName,
                ClassCode = lopHoc!.ClassCode ?? lopHoc.ClassName,
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

        private static string ExamKey(string examId, string classId) => $"{examId}:{classId}";
        private static string GetScoresKey(string examKey) => $"scores:{examKey}";
        private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromHours(3);

        private ConcurrentDictionary<string, double> GetScores(string examKey)
        {
            return _cache.GetOrCreate(GetScoresKey(examKey), entry =>
            {
                entry.SetSlidingExpiration(CacheSlidingExpiration);
                return new ConcurrentDictionary<string, double>();
            });
        }

        // Đề thi : Kết quả thi trắc nghiệm của sinh viên
        [HttpPost]
        public async Task<IActionResult> ExamResult_TN(int maDe, int maLop)
        {
            //check_user
            //Kiểm tra tài khoản của sinh viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users
                .AsNoTracking()
                .Where(us => us.AccessToken == access_token)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FullName
                })
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var deThi = await _context.Exams.FindAsync(maDe);
            if (deThi == null) return NotFound();

            var examHistory = await _context.ExamHistories
                .FirstOrDefaultAsync(ls => ls.UserId == user.UserId && ls.ExamId == maDe);
            if (examHistory == null) return NotFound();

            if (!string.IsNullOrEmpty(examHistory.HangfireJobId))
            {
                BackgroundJob.Delete(examHistory.HangfireJobId);
                examHistory.HangfireJobId = null;
                await _context.SaveChangesAsync();
            }

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
                            var dapAnDung = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionOptionVM>>(cauHoi.ChoiceContent!)!
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
                            .AsNoTracking()
                            .SingleOrDefaultAsync(ch => ch.QuestionId == maCauHoi);
                        if (cauHoi == null) continue;

                        if (cauHoi.QuestionType == "4")
                        {
                            // Lấy đáp án đúng từ ChoiceContent (có thể chứa HTML)
                            var dapAnDungList = Newtonsoft.Json.JsonConvert
                                .DeserializeObject<List<QuestionOptionVM>>(cauHoi.ChoiceContent!)!
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
                            .AsNoTracking()
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
            string orderKey = $"examOptionOrder:{maDe}:{user.UserId}";
            await _redis.RemoveAsync(orderKey);

            try
            {
                var examKey = ExamKey(maDe.ToString(), maLop.ToString());
                var scoreMap = GetScores(examKey);
                scoreMap[user.Username] = viewModel.Score;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving score to IMemoryCache: {ex.Message}");
            }

            await _hubContext.Clients
                .Group($"exam:{maDe}:class:{maLop}")
                .SendAsync("StudentScoreUpdated", new
                {
                    ExamId = maDe.ToString(),
                    ClassId = maLop.ToString(),
                    StudentId = user.Username,
                    StudentName = user.FullName ?? "",
                    Score = viewModel.Score,
                    Duration = viewModel.Duration,
                    ExamHistoryId = examHistory.ExamHistoryId
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
                .AsNoTracking()
                .Include(eh => eh.Exam)
                .FirstOrDefaultAsync(ls => ls.UserId == user.UserId && ls.ExamId == maDe && ls.ClassId == maLop);

            if (examHistory == null)
            {
                TempData["Error"] = "Không có lịch sử làm bài cho đề thi này.";
                return RedirectToAction("ClassDetail", new { id = maLop });
            }

            var studentAnswers = await _context.StudentAnswers
                .AsNoTracking()
                .Where(ct => ct.ExamHistoryId == examHistory.ExamHistoryId)
                .Include(ct => ct.QuestionBank)
                .ToListAsync();


            ViewBag.MaLop = maLop;
            await _hubContext.Clients
                .Group($"exam:{maDe}:class:{maLop}")
                .SendAsync("StudentScoreUpdated", new
                {
                    ExamId = maDe.ToString(),
                    ClassId = maLop.ToString(),
                    StudentId = user.Username,
                    StudentName = user.FullName ?? "",
                    Score = examHistory.Score,
                    Duration = examHistory.Duration,
                    SubmittedAt = DateTime.UtcNow,
                    ExamHistoryId = examHistory.ExamHistoryId
                });

            return View(/* viewModel */);
        }


        public async Task GadingEssayExamByAI(int examHistoryId, int userId)
        {
            var examHistory = await _context.ExamHistories
                .AsNoTracking()
                .Where(e => e.ExamHistoryId == examHistoryId)
                .Select(e => new
                {
                    e.ExamHistoryId,
                    e.UploadedFileId,
                    e.ExamId
                })
                .FirstOrDefaultAsync();

            if (examHistory == null)
                throw new Exception("Không tìm thấy lịch sử làm bài");

            var oldResult = await _context.EssayGradingResults
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.ExamHistoryId == examHistoryId);

            if (oldResult != null && oldResult.Status == "Success")
                throw new Exception("Bài thi đã được chấm trước đó. Không thể chấm lại.");

            var answers = await _context.StudentAnswers
                .AsNoTracking()
                .Include(a => a.QuestionBank)
                .Where(a => a.ExamHistoryId == examHistoryId)
                .ToListAsync();

            if (!answers.Any())
                throw new Exception("Không có câu trả lời tự luận để chấm.");

            var questionIds = answers.Select(a => a.QuestionId).ToList();

            var criteria = await _context.EssayCriterias
                .AsNoTracking()
                .Where(c => questionIds.Contains(c.QuestionId))
                .ToListAsync();

            var matrices = await _context.ExamMatrices
                .AsNoTracking()
                .Where(m => m.ExamId == examHistory.ExamId)
                .ToListAsync();

            float GetMaxScore(int? cloId)
            {
                var m = matrices.FirstOrDefault(x => x.CLOId == cloId);
                if (m == null) return 0;
                return (float)m.CloScore / m.QuestionCount;
            }

            var essayAnswers = answers.Select(a => new
            {
                a.QuestionId,
                a.QuestionBank.QuestionContent,
                a.EssayAnswer,
                MaxScore = GetMaxScore(a.QuestionBank.CLOId),
                EssayCriteria = criteria
                    .Where(c => c.QuestionId == a.QuestionId)
                    .Select(c => new { c.Content, c.Percent })
                    .ToList()
            }).ToList();

            var gradingResult = oldResult ?? new EssayGradingResult
            {
                ExamHistoryId = examHistoryId,
                Status = "Running",
                CreatedAt = DateTime.UtcNow
            };

            if (oldResult == null)
            {
                _context.EssayGradingResults.Add(gradingResult);
                await _context.SaveChangesAsync();
            }
            else
            {
                gradingResult.Status = "Running";
                _context.EssayGradingResults.Update(gradingResult);
                await _context.SaveChangesAsync();
            }

            var examId = examHistory.ExamId;

            var examBankName = await _context.Exams
                .AsNoTracking()
                .Where(e => e.ExamId == examId)
                .Select(e => new
                {
                    e.ExamBank.SubjectName
                })
                .FirstOrDefaultAsync();

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"Hãy đóng vai một giảng viên đại học CHẤM RẤT KHÓ TÍNH, có hơn 10 năm kinh nghiệm đánh giá bài thi tự luận.");
            promptBuilder.AppendLine($"Môn học: {examBankName}");
            promptBuilder.AppendLine("Bạn phải chấm điểm với chuẩn mực học thuật cao, tuyệt đối không dễ dãi. Luôn yêu cầu:");
            promptBuilder.AppendLine("- Đúng kiến thức nền tảng");
            promptBuilder.AppendLine("- Giải thích ngắn gọn, logic chặt chẽ");
            promptBuilder.AppendLine("- Không lan man, không lập lại, không trả lời chung chung");
            promptBuilder.AppendLine("- Nếu bài làm hời hợt hoặc thiếu chi tiết, phải trừ điểm");
            promptBuilder.AppendLine("");

            foreach (var ans in essayAnswers)
            {
                promptBuilder.AppendLine($"Câu hỏi ID: {ans.QuestionId}");
                promptBuilder.AppendLine($"Nội dung câu hỏi: {ans.QuestionContent}");
                promptBuilder.AppendLine($"Điểm tối đa cho câu hỏi với ID {ans.QuestionId} là: {ans.MaxScore}");

                if(ans.EssayCriteria != null)
                {
                    promptBuilder.AppendLine("Tiêu chí:");

                    foreach (var cri in ans.EssayCriteria)
                    {
                        promptBuilder.AppendLine($"- {cri.Content}, chiếm ({cri.Percent}%)");
                    }
                }

                promptBuilder.AppendLine($"Câu trả lời của sinh viên: {ans.EssayAnswer}");
                promptBuilder.AppendLine("---------------------------");
            }

            if (!string.IsNullOrEmpty(examHistory.UploadedFileId))
                promptBuilder.AppendLine($"Sinh viên có nộp kèm file. Hãy kiểm tra rất kỹ vì thông tin bài làm có thể nằm trong file: {examHistory.UploadedFileId}");

            string prompt = promptBuilder.ToString();

            try
            {
                var gradingList = await geminiService.GadingEssayExamAsync(prompt);

                decimal totalScore = 0;

                foreach (var item in gradingList)
                {
                    totalScore += item.Score;

                    var detail = new EssayGradingDetail
                    {
                        GradingResultId = gradingResult.Id,
                        QuestionId = item.QuestionId,
                        Score = item.Score,
                        Comment = item.Comment
                    };

                    _context.EssayGradingDetails.Add(detail);
                }

                gradingResult.TotalScore = totalScore;
                gradingResult.Status = "Success";
                gradingResult.OverallComment = "Chấm bài bằng AI hoàn tất.";

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                gradingResult.Status = "Error";
                gradingResult.OverallComment = ex.Message;
                await _context.SaveChangesAsync();
                throw;
            }
        }


        [HttpPost]
        public async Task<IActionResult> ExamResult_TL([FromBody] EssaySubmitVM model)
        {
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var examHistory = await _context.ExamHistories
                .FirstOrDefaultAsync(l => l.UserId == userId && l.ExamId == model.MaDe && l.ClassId == model.MaLop);
            if (examHistory == null)
            {
                return NotFound(new { message = "Không tìm thấy lịch sử làm bài." });
            }

            if (!string.IsNullOrEmpty(examHistory.HangfireJobId))
            {
                BackgroundJob.Delete(examHistory.HangfireJobId);
                examHistory.HangfireJobId = null;
                await _context.SaveChangesAsync();
            }

            if (!examHistory.SubmitTime.HasValue)
            {
                examHistory.SubmitTime = DateTime.Now;
                examHistory.Duration = (int)(DateTime.Now - examHistory.StartTime).TotalSeconds;
                examHistory.UploadedFileId = model.UploadedFileId;


                var key = $"essayAns:{model.MaDe}:{userId}";
                var savedAnswers = new Dictionary<int, string>();

                try
                {
                    var cacheData = await _redis.GetStringAsync(key);
                    if (!string.IsNullOrEmpty(cacheData))
                    {
                        var redisAnswers = JsonSerializer.Deserialize<Dictionary<string, string>>(cacheData);

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
                }

                var studentAnswers = await _context.StudentAnswers.AsNoTracking()
                    .Where(ct => ct.ExamHistoryId == examHistory.ExamHistoryId)
                    .ToListAsync();

                foreach (var answer in studentAnswers)
                {
                    if (savedAnswers.ContainsKey(answer.QuestionId))
                    {
                        answer.EssayAnswer = savedAnswers[answer.QuestionId];
                        _context.StudentAnswers.Update(answer);
                    }
                }

                _context.ExamHistories.Update(examHistory);
                await _context.SaveChangesAsync();

                try
                {
                    await _redis.RemoveAsync(key);
                    _logger.LogInformation(" REDIS (ExamResult_TL POST): Đã xóa key {Key} sau khi nộp bài.", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, ">LỖI (ExamResult_TL POST): Không thể xóa key Redis {Key}.", key);
                }
            }

            string jobId = BackgroundJob.Enqueue(() =>
                GadingEssayExamByAI(examHistory.ExamHistoryId, userId)
            );

            examHistory.HangfireJobId = jobId;
            await _context.SaveChangesAsync();

            var redirectUrl = Url.Action("ExamResult_TL", "Student", new { maDe = model.MaDe, maLop = model.MaLop});
            return Ok(new { success = true, redirectUrl });
        }


        public async Task<IActionResult> List_History_Detail()
        {
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var historyList = await _context.ExamHistories
                .AsNoTracking()
                .Where(ls => ls.UserId == userId)
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
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var examHistory = await _context.ExamHistories.AsNoTracking()
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
            var exam = await _context.Exams.AsNoTracking()
                .Include(d => d.ExamMatrices)
                .FirstOrDefaultAsync(d => d.ExamId == maDe);

            if (exam == null)
                return NotFound(new { message = "Không tìm thấy đề thi." });

            var questionList = await _context.QuestionBanks.AsNoTracking()
                .Where(c => c.ExamBankId == exam.ExamBankId)
                .ToListAsync();

            var result = questionList.Select((question, index) => new
            {
                id = index + 1,
                category = "HTML",
                question = question.QuestionContent,
                options = Newtonsoft.Json.JsonConvert
                             .DeserializeObject<List<QuestionOptionVM>>(question.ChoiceContent)
                             .Select(lc => lc.ChoiceContent)
                             .ToList()
            });

            return Json(new { questions = result });
        }


    }
}



