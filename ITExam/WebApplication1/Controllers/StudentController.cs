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
using System.Runtime.InteropServices;
using System.Text.Json;
using WebApplication1.Models;
namespace ITExam.Controllers
{
    //[CheckToken]
    [AuthorizeRole("user")]
    public class StudentController : Controller
    {

        private readonly ITExamDbContext _context;
        private readonly IDistributedCache _redis;
        private readonly IHubContext<ExamMonitorHub> _hubContext;


        public StudentController(ITExamDbContext context, IDistributedCache redis, IHubContext<ExamMonitorHub> hubContext)
        {
            _context = context;
            _redis = redis;
            _hubContext = hubContext;
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
            var lopHoc = await _context.Classes
                .Include(l => l.User)
                .Include(l => l.ClassDetails)
                    .ThenInclude(st => st.User)
                .Include(l => l.ClassExams)
                    .ThenInclude(dt => dt.Exam)
                        .ThenInclude(ls => ls.ExamHistories)
                .Include(l => l.ClassExams)
                    .ThenInclude(dt => dt.Exam)
                        .ThenInclude(mt => mt.ExamMatrices)
                .FirstOrDefaultAsync(l => l.ClassId == id);

            if (lopHoc == null)
            {
                return NotFound();
            }

            var danhSachSV = lopHoc.ClassDetails
                .Select(l => l.User)
                .Where(sv => sv != null)
                .Select(sv => new ClassStudentVM
                {
                    StudentId = sv.Username,
                    FullName = sv.FullName
                }).ToList();

            var danhSachDT = lopHoc.ClassExams.Select(ct => new ClassExamVM
            {
                ExamId = ct.Exam.ExamId,
                ExamName = ct.Exam.ExamName,
                Type = ct.IsExam,
                CreatedDate = ct.AddedDate,
                StartDate = ct.StartTime,
                EndDate = ct.EndTime,
                Duration = ct.Exam.Duration,
                ExamType = ct.Exam.ExamType,
                ExamDate = ct.Exam.ExamHistories
                    .FirstOrDefault(ls => ls.UserId == user.UserId && ls.ExamId == ct.Exam.ExamId && ls.ClassId == id)
                    ?.SubmitTime,
                QuestionCount = ct.Exam.ExamMatrices.Sum(sl => sl.QuestionCount)
            }).ToList();

            var viewModel = new ClassDetailVM
            {
                ClassInfo = lopHoc,
                Students = danhSachSV,
                Exams = danhSachDT,
                StudentCount = danhSachSV.Count(),
                ExamCount = danhSachDT.Count(dt => dt.Type == true)
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

            var key = $"exam:{maDe}:{user.UserId}";
            var cacheData = await _redis.GetStringAsync(key);
            var luuLuaChon = string.IsNullOrEmpty(cacheData)
                ? new Dictionary<int, List<int>>()
                : JsonSerializer.Deserialize<Dictionary<int, List<int>>>(cacheData);

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

            string key = $"exam:{data.MaDe}:{user.UserId}";

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

            var key = $"exam:{maDe}:{user.UserId}";
            var cacheData = await _redis.GetStringAsync(key);
            var luuCauTraLoi = string.IsNullOrEmpty(cacheData)
                ? new Dictionary<int, string>()
                : JsonSerializer.Deserialize<Dictionary<int, string>>(cacheData);

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
                Questions = danhSachCauHoi.Select(ch => new EssayQuestionVM
                {
                    QuestionId = ch.QuestionId,
                    QuestionContent = ch.QuestionContent,
                    EssayAnswer = luuCauTraLoi.ContainsKey(ch.QuestionId) ? luuCauTraLoi[ch.QuestionId] : null
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
            var key = $"exam:{request.MaDe}:{request.UserId}";
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

            var lichSuLamBai = await _context.ExamHistories
                .FirstOrDefaultAsync(ls => ls.UserId == user.UserId && ls.ExamId == maDe);
            if (lichSuLamBai == null) return NotFound();

            var key = $"exam:{maDe}:{user.UserId}";
            var cacheData = await _redis.GetStringAsync(key);

            var tongCauHoi = await _context.ExamMatrices
                .Where(mt => mt.ExamId == maDe)
                .SumAsync(mt => mt.QuestionCount);

            double diemMoiCau = 10.0 / tongCauHoi;
            double tongDiem = 0;
            int soCauDung = 0;

            if (string.IsNullOrEmpty(cacheData))
            {
                var vm = new MultipleChoiceExamResultVM
                {
                    Score = Math.Round(0.0, 2),
                    QuestionCount = tongCauHoi,
                    CorrectAnswers = 0,
                    AccuracyRate = 0.0,
                    ExamDate = DateTime.Now,
                    Duration = (int)(DateTime.Now - lichSuLamBai.StartTime).TotalSeconds,
                    ClassId = maLop
                };
                return View(vm);
            }

            var danhSachCauTraLoi = JsonSerializer.Deserialize<Dictionary<int, List<int>>>(cacheData);

            if (danhSachCauTraLoi == null || danhSachCauTraLoi.Count == 0)
            {
                var vm = new MultipleChoiceExamResultVM
                {
                    Score = Math.Round(0.0, 2),
                    QuestionCount = tongCauHoi,
                    CorrectAnswers = 0,
                    AccuracyRate = 0.0,
                    ExamDate = DateTime.Now,
                    Duration = (int)(DateTime.Now - lichSuLamBai.StartTime).TotalSeconds,
                    ClassId = maLop
                };
                return View(vm);
            }

            foreach (var cauTraLoi in danhSachCauTraLoi)
            {
                int maCauHoi = cauTraLoi.Key;
                List<int> luaChonSV = cauTraLoi.Value.OrderBy(x => x).ToList();

                var cauHoi = await _context.QuestionBanks
                    .SingleOrDefaultAsync(ch => ch.QuestionId == maCauHoi);
                if (cauHoi == null) continue;

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

            var viewModel = new MultipleChoiceExamResultVM
            {
                Score = Math.Round(tongDiem, 2),
                QuestionCount = tongCauHoi,
                CorrectAnswers = soCauDung,
                AccuracyRate = tongCauHoi > 0 ? Math.Round((double)soCauDung * 100 / tongCauHoi, 2) : 0,
                ExamDate = DateTime.Now,
                Duration = (int)(DateTime.Now - lichSuLamBai.StartTime).TotalSeconds,
                ClassId = maLop
            };

            // Lưu lịch sử làm bài
            lichSuLamBai.Score = viewModel.Score;
            lichSuLamBai.SubmitTime = DateTime.Now;
            lichSuLamBai.Duration = viewModel.Duration;
            _context.ExamHistories.Update(lichSuLamBai);

            // Lưu chi tiết làm bài
            foreach (var ch in danhSachCauTraLoi)
            {
                var cauHoi = ch.Key;
                var luaChon = ch.Value;

                var chiTietBaiLam = await _context.StudentAnswers
                    .FirstOrDefaultAsync(ct => ct.ExamHistoryId == lichSuLamBai.ExamHistoryId && ct.QuestionId == cauHoi);
                if (chiTietBaiLam != null)
                {
                    chiTietBaiLam.MultipleChoiceAnswer = string.Join(",", luaChon);
                    _context.StudentAnswers.Update(chiTietBaiLam);
                }
            }

            await _context.SaveChangesAsync();

            // Xóa Redis
            await _redis.RemoveAsync(key);

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
                .FirstOrDefaultAsync(ls => ls.UserId == user.UserId && ls.ExamId == maDe && ls.ClassId == maLop);

            if (examHistory == null)
            {
                TempData["Error"] = "Không có lịch sử làm bài cho đề thi này.";
                return RedirectToAction("ClassDetail", new { id = maLop });
            }

            examHistory.SubmitTime = DateTime.Now;
            examHistory.Duration = (int)(DateTime.Now - examHistory.StartTime).TotalSeconds;
            _context.ExamHistories.Update(examHistory);
            await _context.SaveChangesAsync();

            var studentAnswers = await _context.StudentAnswers
                .Where(ct => ct.ExamHistoryId == examHistory.ExamHistoryId)
                .ToListAsync();

            if (studentAnswers.Count == 0)
            {
                TempData["Error"] = "Bạn chưa làm bài hoặc không có lịch sử làm bài cho đề thi này.";
                return RedirectToAction("ClassDetail", new { id = maLop });
            }

            var key = $"exam:{maDe}:{user.UserId}";
            var cacheData = await _redis.GetStringAsync(key);
            var savedAnswers = string.IsNullOrEmpty(cacheData)
                ? new Dictionary<int, string>()
                : JsonSerializer.Deserialize<Dictionary<int, string>>(cacheData);

            foreach (var answer in studentAnswers)
            {
                if (savedAnswers.ContainsKey(answer.QuestionId))
                {
                    answer.EssayAnswer = savedAnswers[answer.QuestionId];
                }
                else
                {
                    answer.EssayAnswer = "";
                }
                _context.StudentAnswers.Update(answer);
            }

            await _context.SaveChangesAsync();

            ViewBag.MaLop = maLop;
            return View();
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
                    ExamId = ls.ExamId,
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

            // Truyền chi tiết lịch sử làm bài vào View
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
    }
}



//Xong het controller => Chia ViewModel ra sua => Tiep tuc sua View => Toc Do, Logic cua action