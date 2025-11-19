using Hangfire;
using ITExam.ExternalModels.Subject;
using ITExam.Filters;
using ITExam.Models;
using ITExam.Services;
using ITExam.ViewModels;
using ITExam.ViewModels.Class;
using ITExam.ViewModels.Class.NewVM;
using ITExam.ViewModels.Exam;
using ITExam.ViewModels.Exam.NewVM;
using ITExam.ViewModels.ExamBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using ChapterCLOVM = ITExam.ViewModels.Exam.ChapterCLOVM;
using CLOItem = ITExam.ViewModels.Exam.CLOItem;
using System.Diagnostics;
using NPOI.XWPF.UserModel;
using System.Text.RegularExpressions;

namespace ITExam.Controllers
{
    //[CheckToken]
    [AuthorizeRole("teacher")]
    public class TeacherController : Controller
    {
        private readonly ITExamDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IClassCodeService _classCodeService;
        private readonly ILogger<TeacherController> _logger;
        public TeacherController(IHttpClientFactory httpClientFactory, ITExamDbContext context, IClassCodeService classCodeService, ILogger<TeacherController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _classCodeService = classCodeService;
            _logger = logger;
        }


        private string? ExtractFileIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    url,
                    @"/d/([a-zA-Z0-9_-]+)"
                );
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                match = System.Text.RegularExpressions.Regex.Match(
                    url,
                    @"[?&]examId=([a-zA-Z0-9_-]+)"
                );
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi parse Google Drive File ID từ URL: {Url}", url);
            }

            _logger.LogWarning("Không thể trích xuất File ID từ URL: {Url}", url);
            return null;
        }

        //================================================================

        #region LopHoc-OK

        //Lớp học : Danh sách lớp học - OK
        public async Task<IActionResult> Class()
        {
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var viewModel = await _context.Classes
                .AsNoTracking()
                .Where(cl => cl.UserId == userId)
                .Select(cl => new ClassVM
                {
                    ClassId = cl.ClassId,
                    ClassName = cl.ClassName,
                    CreatedDate = cl.CreatedDate,
                    StudentCount = cl.ClassDetails.Count(),
                    ExamCount = cl.ClassExams.Count(ex => ex.IsExam == true), //Sua sau
                    Description = cl.Description
                }).ToListAsync();
            return View(viewModel);
        }


        //Lớp học : Chỉnh sửa lớp học - OK
        [HttpPost]
        public async Task<IActionResult> EditClass(EditClassVM model)
        {
            var classEdit = await _context.Classes.FindAsync(model.ClassId);
            if (classEdit == null)
            {
                return NotFound();
            }
            classEdit.ClassName = model.ClassName;
            classEdit.Description = model.Description;

            _context.Update(classEdit); //Sua sau
            await _context.SaveChangesAsync();

            return RedirectToAction("Class");
        }


        //Lớp học : Thêm lớp học - OK
        [HttpPost]
        public async Task<IActionResult> Class(Class model)
        {
            //Kiểm tra tài khoản giảng viên
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            model.CreatedDate = DateTime.Now;
            model.UserId = userId;
            model.ClassCode = await _classCodeService.GenerateUniqueClassCodeAsync();

            _context.Classes.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm lớp học thành công!";
            return RedirectToAction("Class");
        }


        //Lớp học : Chi tiết lớp học - OK
        public async Task<IActionResult> ClassDetail(int id)
        {
            // Kiểm tra tài khoản giảng viên
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(cl => cl.ClassId == id)
                .Select(cl => new ClassInfoVM
                {
                    ClassId = cl.ClassId,
                    ClassName = cl.ClassName,
                    ClassCode = cl.ClassCode,
                    CreatedDate = cl.CreatedDate,
                    Description = cl.Description,
                    UserId = cl.UserId,
                    UserFullName = cl.User.FullName,
                })
                .FirstOrDefaultAsync();

            if (classInfo is null)
                return NotFound();

            // Danh sách sinh viên
            var studentListOfClass = await _context.ClassDetails
                .AsNoTracking()
                .Where(cd => cd.ClassId == id)
                .OrderBy(cd => cd.JoinDate)
                .Select(cd => new ClassStudentVM
                {
                    StudentId = cd.User.Username,
                    FullName = cd.User.FullName,
                    Email = cd.User.Email,
                    JoinDate = cd.JoinDate
                })
                .ToListAsync();

            // Danh sách đề thi đã thêm vào lớp
            var examsInClass = await _context.ClassExams
                .AsNoTracking()
                .Where(ec => ec.ClassId == id)
                .OrderBy(ce => ce.AddedDate)
                .Select(ce => new ClassExamVM
                {
                    ExamId = ce.ExamId,
                    ExamName = ce.Exam.ExamName,
                    Duration = ce.Exam.Duration,
                    AddedDate = ce.AddedDate,
                    StartTime = ce.StartTime,
                    EndTime = ce.EndTime,
                    QuestionCount = ce.Exam.ExamMatrices.Sum(m => m.QuestionCount),
                    Access = ce.Access ?? false,
                    ExamType = ce.Exam.ExamType,
                    UngradedStudentCount =
                        ce.Exam.ExamType == "Tự luận" && ce.Exam.ExamHistories != null
                        ? ce.Exam.ExamHistories
                            .Count(ls => ls.ClassId == id && ls.SubmitTime != null && ls.Score == null)
                        : 0
                })
                .ToListAsync();

            // Danh sách đề thi chưa thêm vào lớp
            var unassignedExams = await _context.Exams
                .AsNoTracking()
                .Where(e => e.UserId == userId
                            && !_context.ClassExams.Any(ce => ce.ExamId == e.ExamId && ce.ClassId == id))
                .OrderByDescending(e => e.CreatedDate)
                .Select(e => new UnassignedExamVM
                {
                    InstructorId = e.UserId,
                    ExamId = e.ExamId,
                    ExamName = e.ExamName,
                    Duration = e.Duration,
                    CreatedDate = e.CreatedDate,
                    QuestionCount = e.ExamMatrices.Select(m => (int?)m.QuestionCount).Sum() ?? 0
                })
                .ToListAsync();


            var logs = await _context.ActivityLogs
                .AsNoTracking()
                .Where(l => l.ClassId == id)
                .OrderByDescending(l => l.LogDate)
                .Select(l => new ExamLogEntryVM
                {
                    LogId = l.ActivityLogId,
                    StudentFullName = l.User.FullName,
                    InstructorName = l.InstructorName!,
                    ExamName = l.Exam.ExamName,
                    ClassName = l.Class.ClassName,
                    LoggedDate = l.LogDate,
                    LogContent = l.LogContent!
                })
                .ToListAsync();

            var viewModel = new ClassDetailVM
            {
                ClassInfo = classInfo,
                Students = studentListOfClass,
                Logs = logs,
                Exams = examsInClass,
                UnassignedExams = unassignedExams,
                StudentCount = studentListOfClass.Count,
                ExamCount = examsInClass.Count,
                ClassCode = classInfo.ClassCode
            };

            return View(viewModel);
        }


        //Lớp học : Danh sách sinh viên đã làm bài
        public async Task<IActionResult> List_Student_Do_Test(int examId, int classId)
        {
            // Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var thisClass = await _context.Classes
            .Include(l => l.ClassDetails)
            .ThenInclude(st => st.User)
            .Include(ct => ct.ClassExams)
            .ThenInclude(dt => dt.Exam)
            .ThenInclude(ls => ls.ExamHistories)
            .FirstOrDefaultAsync(l => l.ClassId == classId);

            if (thisClass == null || thisClass.ClassDetails == null || thisClass.ClassExams == null)
                return NotFound();

            var isExamInClass = thisClass.ClassExams
                .Any(deThi => deThi.ClassId == classId && deThi.ExamId == examId);

            if (!isExamInClass)
            {
                return NotFound("Đề thi không tồn tại trong lớp học này.");
            }

            var danhSachSV = thisClass.ClassDetails
            .Select(l => l.User)
            .Where(sv => sv != null)
            .Select(sv =>
            {
                var lichSu = thisClass.ClassExams
                    .SelectMany(d => d.Exam.ExamHistories)
                    .FirstOrDefault(ls => ls.UserId == sv.UserId && ls.ExamId == examId && ls.ClassId == classId);

                return lichSu != null ? new StudentSubmissionInClassVM
                {
                    StudentId = sv.Username,
                    FullName = sv.FullName,
                    StartTime = lichSu.StartTime,
                    SubmitTime = lichSu.SubmitTime ?? DateTime.MinValue,
                    Duration = lichSu.Duration,
                    Score = lichSu.Score,
                    ExamHistoryId = lichSu.ExamHistoryId
                } : null;
            })
            .Where(vm => vm != null)
            .ToList();

            var viewModel = new StudentSubmissionList
            {
                ClassInfo = thisClass,
                Students = danhSachSV!
            };

            return View(viewModel);
        }


        //Lớp học: Xóa lớp học
        [HttpPost]
        public async Task<IActionResult> DeleteClass(int maLopHoc)
        {
            var lopHoc = await _context.Classes.AsNoTracking()
                .FirstOrDefaultAsync(l => l.ClassId == maLopHoc);

            if (lopHoc == null)
                return NotFound();

            string classCodeFolder = lopHoc.ClassCode.ToString();

            var historyIds = await _context.ExamHistories
                .AsNoTracking()
                .Where(ls => ls.ClassId == maLopHoc)
                .Select(ls => ls.ExamHistoryId)
                .ToListAsync();

            if (historyIds.Count > 0)
            {
                await _context.StudentAnswers
                    .AsNoTracking()
                    .Where(sa => historyIds.Contains(sa.ExamHistoryId))
                    .ExecuteDeleteAsync();

                await _context.ExamHistories
                    .AsNoTracking()
                    .Where(ls => historyIds.Contains(ls.ExamHistoryId))
                    .ExecuteDeleteAsync();
            }

            await _context.ActivityLogs
                .AsNoTracking()
                .Where(l => l.ClassId == maLopHoc)
                .ExecuteDeleteAsync();

            await _context.ClassDetails
                .AsNoTracking()
                .Where(cd => cd.ClassId == maLopHoc)
                .ExecuteDeleteAsync();

            await _context.ClassExams
                .AsNoTracking()
                .Where(ce => ce.ClassId == maLopHoc)
                .ExecuteDeleteAsync();

            _context.Classes.Remove(lopHoc);
            await _context.SaveChangesAsync();

            try
            {
                var encodedClassCode = Uri.EscapeDataString(classCodeFolder);

                var finalUrl = $"{RoutingAPI.AppScriptUrl}?action=deleteClass&classCode={encodedClassCode}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(finalUrl);
                }
                TempData["SuccessMessage"] = "Đã xóa lớp học, dữ liệu và toàn bộ thư mục bài làm thành công!";
            }
            catch (Exception ex)
            {
                TempData["WarningMessage"] = $"Đã xóa lớp học khỏi DB, nhưng gặp lỗi khi gọi Drive API: {ex.Message}";
            }

            return RedirectToAction("Class");
        }


        #endregion

        //================================================================

        #region DeThi
        //Đề thi : Danh sách đề thi - OK
        public async Task<IActionResult> Exam()
        {
            //Kiểm tra tài khoản giảng viên
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var exams = await _context.Exams
                .AsNoTracking()
                .Where(d => d.UserId == userId)
                .Select(d => new ExamListVM
                {
                    ExamId = d.ExamId,
                    ExamName = d.ExamName,
                    ExamType = d.ExamType,
                    Duration = d.Duration,
                    TotalQuestions = d.ExamMatrices.Sum(m => (int?)m.QuestionCount) ?? 0,
                    IsUsedInClass = d.ClassExams.Any()
                })
                .ToListAsync();

            return View(exams);
        }


        //Đề thi : Tạo đề thi từ ngân hàng đề - OK
        public async Task<IActionResult> Exam_Create()
        {
            //Kiểm tra tài khoản giảng viên
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var nganHangDeListCuaToi = await _context.ExamBanks
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .Select(n => new CreateNewExamVM
                {
                    ExamBankId = n.ExamBankId,
                    ExamBankName = n.ExamBankName,
                    ExamType = n.ExamType
                })
                .ToListAsync();

            var nganHangDeListCongKhai = await _context.ExamBanks
                .AsNoTracking()
                .Where(n => n.Status == true)
                .Select(n => new CreateNewExamVM
                {
                    ExamBankId = n.ExamBankId,
                    ExamBankName = n.ExamBankName,
                    ExamType = n.ExamType
                })
                .ToListAsync();

            ViewData["NganHangDeListCuaToi"] = nganHangDeListCuaToi;
            ViewData["NganHangDeListCongKhai"] = nganHangDeListCongKhai;

            return View();
        }


        //Đề thi : Lưu đề thi đã tạo từ ngân hàng đề - OK
        [HttpPost]
        public async Task<IActionResult> Exam_Create(Exam deThiMoi)
        {
            //Kiểm tra tài khoản giảng viên
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var nganHangDe = await _context.ExamBanks.AsNoTracking().FirstOrDefaultAsync(n => n.ExamBankId == deThiMoi.ExamBankId);
            if (nganHangDe == null)
            {

                return RedirectToAction("Error", "Home");
            }
            deThiMoi.UserId = userId;
            deThiMoi.CreatedDate = DateTime.Now;
            deThiMoi.ExamType = nganHangDe.ExamType;
            _context.Add(deThiMoi);

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã tạo đề thi thành công!";
            return RedirectToAction("Exam_Create_Matrix", new { id = deThiMoi.ExamId });
        }


        //Đề thi : Tạo ma trận đề thi, lấy câu hỏi từ ngân hàng đề - OK-
        public async Task<IActionResult> Exam_Create_Matrix(int id)
        {
            // Lấy đề thi & ngân hàng đề
            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.ExamId == id)
                .Select(e => new
                {
                    e.ExamId,
                    e.ExamName,
                    e.Duration,
                    ExamBankId = e.ExamBank.ExamBankId,
                    SubjectId = e.ExamBank.SubjectId
                })
                .FirstOrDefaultAsync();

            if (exam == null)
            {
                return NotFound();
            }

            var subjectId = exam.SubjectId;
            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Không thể lấy danh sách môn học.");
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root!["data"];
            var subjectList = dataToken!.ToObject<List<SubjectDto>>();
            var subject = subjectList!.FirstOrDefault(s => s.Id == subjectId);
            var subjectName = subject?.Name;

            var questionBanks = await _context.QuestionBanks
                .AsNoTracking()
                .Where(q => q.ExamBankId == exam.ExamBankId)
                .Select(q => new { q.ChapterId, q.CLOId })
                .ToListAsync();

            var chapterItem = subject?.Chapters.Select(c => new
            {
                ChuongId = c.Id,
                TenChuong = c.Title,
                Clos = c.Clos.Select(clo => new
                {
                    CLOId = clo.Id,
                    CLOTitle = clo.Title,
                    MaxQuestionCount = questionBanks
                                    .Count(ch => ch.ChapterId == c.Id && ch.CLOId == clo.Id)
                }).ToList()
            }).ToList();

            var matrices = await _context.ExamMatrices
                .AsNoTracking()
                .Where(m => m.ExamId == id)
                .ToListAsync();

            var chapters = subject?.Chapters.Select(ch => new ChapterCLOVM
            {
                ChapterId = ch.Id,
                ChapterTitle = ch.Title,
                CLOs = ch.Clos.Select(clo => new CLOItem
                {
                    CLOId = clo.Id,
                    CLO = clo.Title,
                    QuestionCount = matrices
                      .FirstOrDefault(m => m.ChapterId == ch.Id && m.CLOId == clo.Id)
                      ?.QuestionCount ?? 0,
                    MaxQuestionCount = questionBanks
                    .Count(c => c.ChapterId == ch.Id && c.CLOId == clo.Id)
                }).ToList()
            }).ToList();

            var viewModel = new ExamMatrixVM
            {
                ExamId = id,
                ExamName = exam.ExamName,
                Duration = exam.Duration,
                Chapters = chapters!
            };

            ViewBag.SubjectName = subjectName;
            ViewData["ChapterJson"] = JsonConvert.SerializeObject(chapterItem);
            return View(viewModel);
        }


        //Đề thi : Chỉnh sửa đề thi - OK
        [HttpPost]
        public async Task<IActionResult> EditExam(int MaDe, string TenDeThi, int ThoiGianLamBai)
        {
            var deThi = await _context.Exams.FirstOrDefaultAsync(d => d.ExamId == MaDe);
            if (deThi == null)
            {
                return NotFound("Không tìm thấy đề thi.");
            }
            deThi.ExamName = TenDeThi;
            deThi.Duration = ThoiGianLamBai;

            _context.Exams.Update(deThi); //Sua sau
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật đề thi thành công.";
            return RedirectToAction("Exam_Create_Matrix", new { id = MaDe });
        }


        //Đề thi : Lưu ma trận đề thi - OK-
        [HttpPost]
        public async Task<IActionResult> Save_Exam_Matrix([FromBody] ExamMatrixVM model)
        {
            // Kiểm tra xem model có hợp lệ không
            if (model == null || model.Chapters == null)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            var oldMatrix = await _context.ExamMatrices
                .Where(m => m.ExamId == model.ExamId)
                .ToListAsync();

            _context.ExamMatrices.RemoveRange(oldMatrix);
            await _context.SaveChangesAsync();

            // Lưu ma trận đề thi mới
            foreach (var chuong in model.Chapters)
            {
                foreach (var clo in chuong.CLOs)
                {
                    if (clo.QuestionCount > 0)
                    {
                        var item = new ExamMatrix
                        {
                            ExamId = model.ExamId,
                            ChapterId = chuong.ChapterId,
                            CLOId = clo.CLOId,
                            QuestionCount = clo.QuestionCount
                        };
                        _context.ExamMatrices.Add(item);
                    }
                }
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true, deThiId = model.ExamId });
        }


        //Đề thi : Cập nhật đề thi - OK-
        [HttpPost]
        public async Task<IActionResult> Update_Exam([FromBody] ExamMatrixVM model)
        {
            //Truy vấn đề thi theo ID
            var exam = await _context.Exams.FirstOrDefaultAsync(d => d.ExamId == model.ExamId);
            if (exam == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đề thi." });
            }
            //Cập nhật thời gian làm bài
            exam.Duration = model.Duration;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        //Đề thi : Thêm đề thi vào lớp học - OK-
        [HttpPost]
        public async Task<IActionResult> Add_Exam_To_Class(ExamUnlockTimeVM vm, int maDe, int maLopHoc)
        {
            var exam = await _context.Exams
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.ExamId == maDe);
            var @class = await _context.Classes
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ClassId == maLopHoc);
            if (exam == null || @class == null)
            {
                return NotFound();
            }

            var newExam = new ClassExam
            {
                ClassId = maLopHoc,
                ExamId = maDe,
                IsExam = vm.LaDeThi,
                AddedDate = DateTime.Now,
                StartTime = vm.StartTime,
                EndTime = vm.EndTime,
                Access = false // mặc định
            };
            if (exam.ExamType == "Tự luận")
            {
                newExam.Access = true;
            }

            _context.ClassExams.Add(newExam);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = newExam.IsExam ? "Thêm đề thi vào lớp học thành công!" : "Thêm đề ôn tập vào lớp học thành công!";

            var students = await _context.ClassDetails
                .AsNoTracking()
                .Where(lh => lh.ClassId == maLopHoc)
                .Select(s => new
                {
                    FullName = s.User.FullName,
                    ClassName = s.Class.ClassName,
                    Email = s.User.Email
                })
                .ToListAsync();

            if (newExam.IsExam)
            {
                foreach (var st in students)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append(@"<!DOCTYPE html>
                        <html lang=""vi"">
                        <head>
                            <meta charset=""UTF-8"">
                            <title>Thông báo đề thi mới</title>
                            <style>
                                @keyframes fadeIn {
                                    from { opacity: 0; transform: translateY(10px); }
                                    to { opacity: 1; transform: translateY(0); }
                                }

                                .fade-in {
                                    animation: fadeIn 0.8s ease-out;
                                }
                            </style>
                        </head>
                        <body style=""margin: 0; padding: 0; background-color: #f2f4f7; font-family: 'Segoe UI', Tahoma, sans-serif;"">
                            <div style=""max-width: 640px; margin: 40px auto; background: #ffffff; border-radius: 12px; box-shadow: 0 8px 20px rgba(0,0,0,0.05); overflow: hidden;"">
        
                                <div style=""background-color: #0051cc; color: #fff; padding: 24px 32px;"">
                                    <h2 style=""margin: 0; font-size: 26px;"">ITEXAM - THÔNG BÁO ĐỀ THI MỚI</h2>
                                </div>

                                <div class=""fade-in"" style=""padding: 32px;"">
                                    <p style=""font-size: 16px; margin-bottom: 16px;"">Xin chào <strong>" + st.FullName + @"</strong>,</p>

                                    <p style=""font-size: 16px; line-height: 1.6;"">
                                        Một đề thi mới đã được giảng viên thêm vào lớp học <strong>" + st.ClassName + @"</strong> mà bạn đang tham gia.
                                    </p>

                                    <table style=""width: 100%; border-collapse: collapse; margin-top: 24px; font-size: 15px;"">
                                        <tr style=""background-color: #eef2f7;"">
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Tên đề thi</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + exam.ExamName + @"</td>
                                        </tr>
                                        <tr>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Thời gian làm bài</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + exam.Duration + @" phút</td>
                                        </tr>
                                        <tr style=""background-color: #eef2f7;"">
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Ngày bắt đầu</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + newExam.StartTime.ToString("dd/MM/yyyy HH:mm:ss") + @"</td>
                                        </tr>
                                        <tr>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Ngày kết thúc</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + newExam.EndTime.ToString("dd/MM/yyyy HH:mm:ss") + @"</td>
                                        </tr>
                                    </table>

                                    <div style=""margin-top: 28px;"">
                                        <p style=""font-size: 16px;"">Vui lòng đăng nhập vào hệ thống <strong>ITExam</strong> để làm bài đúng thời gian quy định.</p>
                                    </div>
                                </div>

                                <div style=""background-color: #f0f1f3; text-align: center; padding: 18px; font-size: 13px; color: #666;"">
                                    Đây là email tự động từ hệ thống ITExam. Vui lòng không trả lời email này.
                                </div>
                            </div>
                        </body>
                        </html>");

                    BackgroundJob.Enqueue<EmailService>(service => service.SendAddExamNotification(st.Email!, sb.ToString(), true));

                }
            }

            return RedirectToAction("ClassDetail", new { id = maLopHoc });
        }


        //Đề thi : Lưu dữ liệu thêm đề thi - OK-
        [HttpPost]
        public async Task<IActionResult> Edit_Exam_Date(ExamUnlockTimeVM vm, int maDe, int maLopHoc)
        {
            if (ModelState.IsValid)
            {
                var classExam = await _context.ClassExams
                    .FirstOrDefaultAsync(x => x.ExamId == maDe);
                if (classExam != null)
                {
                    classExam.StartTime = vm.StartTime;
                    classExam.EndTime = vm.EndTime;

                    await _context.SaveChangesAsync();
                }
                else
                {
                    return NotFound("Không tìm tìm thấy đề để thay đổi");
                }
            }
            TempData["SuccessMessage"] = "Cập nhật thành công.";
            return RedirectToAction("ClassDetail", new { id = maLopHoc });
        }


        //Đề thi : Xóa đề thi trong một lớp học - OK
        [HttpPost]
        public async Task<IActionResult> Delete_Exam(ExamUnlockTimeVM vm, int maDe, int maLopHoc)
        {
            if (!ModelState.IsValid)
                return RedirectToAction("ClassDetail", new { id = maLopHoc });

            var classExamInfo = await _context.ClassExams.AsNoTracking()
                .Where(dt => dt.ExamId == maDe && dt.ClassId == maLopHoc)
                .Select(ce => new
                {
                    ClassCode = ce.Class.ClassCode.ToString(), 
                    ExamName = ce.Exam.ExamName.ToString()        
                })
                .FirstOrDefaultAsync();

            if (classExamInfo == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đề thi để xóa.";
                return RedirectToAction("ClassDetail", new { id = maLopHoc });
            }

            string classCodeFolder = classExamInfo.ClassCode;
            string examNameFolder = classExamInfo.ExamName;

            var historyIds = await _context.ExamHistories
                .AsNoTracking()
                .Where(ls => ls.ExamId == maDe && ls.ClassId == maLopHoc)
                .Select(ls => ls.ExamHistoryId)
                .ToListAsync();

            if (historyIds.Count > 0)
            {
                await _context.StudentAnswers
                    .AsNoTracking()
                    .Where(sa => historyIds.Contains(sa.ExamHistoryId))
                    .ExecuteDeleteAsync();

                await _context.ExamHistories
                    .AsNoTracking()
                    .Where(ls => historyIds.Contains(ls.ExamHistoryId))
                    .ExecuteDeleteAsync();
            }

            await _context.ClassExams
                .AsNoTracking()
                .Where(c => c.ExamId == maDe && c.ClassId == maLopHoc)
                .ExecuteDeleteAsync();

            try
            {
                var encodedClassCode = Uri.EscapeDataString(classCodeFolder);
                var encodedExamName = Uri.EscapeDataString(examNameFolder);

                var finalUrl = $"{RoutingAPI.AppScriptUrl}?action=deleteExam&classCode={encodedClassCode}&examName={encodedExamName}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(finalUrl);

                }
                TempData["SuccessMessage"] = "Đã xóa đề thi, lịch sử làm bài khỏi lớp học ";

            }
            catch (Exception ex)
            {
                TempData["WarningMessage"] = $"Đã xóa đề thi khỏi database, nhưng không thể xóa thư mục bài làm trên Drive. Vui lòng xóa thủ công. Lỗi: {ex.Message}";
            }
            return RedirectToAction("ClassDetail", new { id = maLopHoc });
        }


        //Đề thi : Xóa đề thi khỏi database
        [HttpPost]
        public async Task<IActionResult> XoaDeThi(int examId) //Sua sau
        {
            var exam = await _context.Exams
                .Include(e => e.ExamHistories)
                .ThenInclude(h => h.StudentAnswers)
                .Include(e => e.ExamMatrices)
                .Include(e => e.ClassExams)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null)
            {
                return NotFound("Không tìm thấy đề thi.");
            }

            if (exam.ExamHistories != null)
            {
                foreach (var history in exam.ExamHistories)
                {
                    if (history.StudentAnswers != null)
                    {
                        _context.StudentAnswers.RemoveRange(history.StudentAnswers);
                    }
                }

                _context.ExamHistories.RemoveRange(exam.ExamHistories);
            }

            if (exam.ExamMatrices != null)
                _context.ExamMatrices.RemoveRange(exam.ExamMatrices);

            if (exam.ClassExams != null)
                _context.ClassExams.RemoveRange(exam.ClassExams);

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa đề thi thành công!";
            return RedirectToAction("Exam");
        }


        //Đề thi : Chi tiết lịch sử làm bài của sinh viên - OK
        public async Task<IActionResult> Exam_History_Detail(int id)
        {
            var history = await _context.ExamHistories
                .AsNoTracking()
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
                        ChoiceContent = ct.QuestionBank.ChoiceContent!,
                        QuestionType = ct.QuestionBank.QuestionType!,
                        MultipleChoiceAnswer = ct.MultipleChoiceAnswer!,
                        EssayAnswer = ct.EssayAnswer!,
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (history == null)
            {
                return NotFound("Không tìm thấy lịch sử làm bài.");
            }

            return View(history);
        }


        // Đề thi : Xóa lịch sử làm bài của sinh viên   
        [HttpPost]
        public async Task<IActionResult> Delete_History_Exam(int id)
        {
            var lichSu = await _context.ExamHistories
                .Include(l => l.StudentAnswers)
                .FirstOrDefaultAsync(l => l.ExamHistoryId == id);

            if (lichSu == null)
            {
                return NotFound("Không tìm thấy lịch sử làm bài.");
            }
            int maDe = lichSu.ExamId;
            int maLopHoc = lichSu.ClassId;
            var fileUrlToDelete = lichSu.UploadedFileId;

            if (!string.IsNullOrEmpty(fileUrlToDelete))
            {
                var fileIdToDelete = ExtractFileIdFromUrl(fileUrlToDelete);

                if (!string.IsNullOrEmpty(fileIdToDelete))
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient();
                        var deleteUrl = $"{RoutingAPI.AppScriptUrl}?action=delete&fileId={fileIdToDelete}";

                        var response = await client.GetAsync(deleteUrl);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("LỖI (Delete_History_Exam): Google Apps Script (File ID: {FileId}) trả về lỗi. Status: {StatusCode}", fileIdToDelete, response.StatusCode);
                        }
                        else
                        {
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation("(Delete_History_Exam): Phản hồi Google Apps Script: {Response}", jsonResponse);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "LỖI (Delete_History_Exam): Không thể gọi Google Apps Script để xóa File ID: {FileId}", fileIdToDelete);
                    }
                }
                else
                {
                    _logger.LogWarning("(Delete_History_Exam): Không tìm thấy File ID trong URL: {Url}", fileUrlToDelete);
                }
            }
            _context.StudentAnswers.RemoveRange(lichSu.StudentAnswers);
            _context.ExamHistories.Remove(lichSu);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa lịch sử làm bài thành công!";
            return RedirectToAction("List_Student_Do_Test", new { examId = maDe, classId = maLopHoc });
        }


        // Đề thi : Update trạng thái xem lịch sử làm bài
        [HttpPost]
        public async Task<IActionResult> CapNhatAccess([FromBody] UpdateAccessVM model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Model null từ client" });
            }

            var classExam = await _context.ClassExams
                .FirstOrDefaultAsync(x => x.ClassId == model.ClassId && x.ExamId == model.ExamId);

            if (classExam == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đề thi trong lớp." });
            }

            classExam.Access = model.Access;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật quyền truy cập thành công!" });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Lỗi server khi lưu dữ liệu!" });
            }
        }


        // Đề thi : Danh sách sinh viên của đề thi tự luận cần chấm bài - OK
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Grading_The_Exam(int maDe, int maLop)
        {
            var allStudentsInClass = await _context.ClassDetails
                .AsNoTracking()
                .Where(ct => ct.ClassId == maLop)
                .Select(ct => ct.User)
                .ToListAsync();

            var examHistories = await _context.ExamHistories
                .AsNoTracking()
                .Where(ls => ls.ExamId == maDe && ls.ClassId == maLop)
                .Select(ls => new {
                    ls.ExamHistoryId,
                    ls.UserId,
                    ls.SubmitTime,
                    ls.Duration,
                    ls.Score,
                    FullName = ls.User.FullName,
                    Username = ls.User.Username
                })
                .ToListAsync();

            var submittedStudents = examHistories.Select(sv => new StudentExamVM
            {
                UserId = sv.UserId,
                HistoryId = sv.ExamHistoryId,
                FullName = sv.FullName,
                StudentID = sv.Username,
                SubmissionTime = sv.SubmitTime,
                DurationInMinutes = sv.Duration,
                GradedScore = sv.Score
            }).ToList();

            var submittedUserIds = examHistories.Select(ls => ls.UserId).ToHashSet();
            var unsubmittedStudents = allStudentsInClass
                .Where(u => !submittedUserIds.Contains(u.UserId))
                .Select(u => new StudentExamVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    StudentID = u.Username,
                    SubmissionTime = null,
                    DurationInMinutes = 0,
                    GradedScore = null
                }).ToList();

            var viewModel = new GradingVM
            {
                ExamId = maDe,
                ClassId = maLop,
                NumberOfUngradedStudents = examHistories.Count(ls => ls.SubmitTime != null && ls.Score == null),
                SubmittedStudents = submittedStudents,
                UnsubmittedStudents = unsubmittedStudents
            };

            return View(viewModel);
        }

        [HttpGet]
        [HttpPost]
        //Đề thi: Chấm bài một sinh viên
        public async Task<IActionResult> Grading_The_Exam_Of_Student(int id)
        {
            var history = await _context.ExamHistories
                .Include(ls => ls.User)
                .Include(ls => ls.Exam)
                .Include(ls => ls.StudentAnswers)
                    .ThenInclude(ct => ct.QuestionBank)
                .FirstOrDefaultAsync(ls => ls.ExamHistoryId == id);

            if (history == null) return NotFound();

            var vm = new EssaySubmissionToGradeVM
            {
                HistoryId = history.ExamHistoryId,
                FullName = history.User.FullName,
                StudentID = history.User.Username,
                ExamTitle = history.Exam.ExamName,
                SubmissionTime = history.SubmitTime,
                ListQuestions = history.StudentAnswers.Select(ct => new GradedEssayQuestionVM
                {
                    QuestionId = ct.QuestionId,
                    QuestionContent = ct.QuestionBank.QuestionContent,
                    StudentAnswer = ct.EssayAnswer!,
                    GradedScore = ct.Score
                }).ToList(),
                UploadedFileLink = history.UploadedFileId,
                ExamId = history.ExamId,
                ClassId = history.ClassId,
            };

            if (!string.IsNullOrEmpty(history.UploadedFileId))
            {
                _logger.LogInformation("Đã có file đính kèm cho HistoryId {HistoryId}: {FileUrl}",
                    id, history.UploadedFileId);
            }
            else
            {
                _logger.LogInformation("Sinh viên không nộp file (UploadedFileId rỗng) cho HistoryId: {HistoryId}", id);
            }

            return View(vm);
        }


        //Đề thi : Lưu điểm chấm tự luận 
        [HttpPost]
        public async Task<IActionResult> SaveGradingResult(EssaySubmissionToGradeVM model)
        {
            var history = await _context.ExamHistories.Include(ls => ls.StudentAnswers)
                .FirstOrDefaultAsync(ls => ls.ExamHistoryId == model.HistoryId);

            if (history == null)
                return NotFound("Không tìm thấy bài làm");

            double totalScore = 0;
            int totalQuestions = model.ListQuestions.Count;

            foreach (var question in model.ListQuestions)
            {
                var answer = history.StudentAnswers.FirstOrDefault(ct => ct.QuestionId == question.QuestionId);
                if (answer != null)
                {
                    answer.Score = question.GradedScore;
                    totalScore += question.GradedScore ?? 0;
                }
            }

            double maxPossibleScore = totalQuestions * 10;

            double normalizedScore = (totalScore / maxPossibleScore) * 10;

            Console.WriteLine($"Total Score: {totalScore}, Max Possible Score: {maxPossibleScore}, Normalized Score: {normalizedScore}");

            history.Score = Math.Round(normalizedScore, 2);
            history.SubmitTime = history.SubmitTime ?? DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Chấm điểm thành công!";
            return RedirectToAction("Grading_The_Exam", new { maDe = history.ExamId, maLop = history.ClassId });
        }


        #endregion

        //================================================================

        #region NganHangDe


        //Ngân hàng đề : Danh sách ngân hàng đề - OK
        public async Task<IActionResult> ExamBank()
        {
            // Kiểm tra tài khoản giảng viên
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var privateExamBanks = await _context.ExamBanks
                .AsNoTracking()
                .Where(nh => nh.UserId == userId)
                .Select(nh => new PrivateExamBankVM
                {
                    ExamBankId = nh.ExamBankId,
                    ExamBankName = nh.ExamBankName,
                    CreatedDate = nh.CreatedDate,
                    IsActive = nh.Status,
                    ExamType = nh.ExamType
                })
                .ToListAsync();

            var publicExamBanks = await _context.ExamBanks
                .AsNoTracking()
                .Where(nh => nh.Status == true)
                .Select(nh => new PublicExamBankVM
                {
                    ExamBankId = nh.ExamBankId,
                    CreatedDate = nh.CreatedDate,
                    ExamBankName = nh.ExamBankName,
                    ExamType = nh.ExamType,
                    LecturerId = nh.UserId,
                    LecturerName = nh.User.FullName
                })
                .ToListAsync();

            var viewModel = new ExamBankListVM
            {
                PrivateExamBanks = privateExamBanks,
                PublicExamBanks = publicExamBanks
            };

            return View(viewModel);
        }


        //Ngân hàng đề : Chi tiết ngân hàng đề - OK
        public async Task<IActionResult> Exam_Bank_Detail(int id)
        {
            // Truy vấn ngân hàng đề
            var examBank = await _context.ExamBanks.FindAsync(id);
            if (examBank == null)
                return NotFound("Không tìm thấy ngân hàng đề.");

            int subjectId = examBank.SubjectId;
            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Không thể lấy dữ liệu môn học.");
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root!["data"];
            var subjectList = dataToken!.ToObject<List<SubjectDto>>();
            var monHoc = subjectList!.FirstOrDefault(s => s.Id == subjectId);
            if (monHoc == null) return NotFound();
            var danhSachChuong = monHoc?.Chapters ?? new List<ChapterDto>();

            var examBankInfo = await _context.ExamBanks
                .AsNoTracking()
                .Where(ch => ch.ExamBankId == id)
                .Select(ch => new
                {
                    ch.ExamBankName,
                    ch.CreatedDate,
                    ch.ExamType
                })
                .FirstOrDefaultAsync();

            var examQuestions = await _context.QuestionBanks
                .AsNoTracking()
                .Where(q => q.ExamBankId == id)
                .Select(q => new ExamBankQuestionVM
                {
                    QuestionId = q.QuestionId,
                    QuestionContent = q.QuestionContent,
                    Options = q.ChoiceContent,
                    QuestionScore = q.QuestionScore,
                    QuestionType = q.QuestionType,
                    ChapterId = q.ChapterId,
                    CloId = q.CLOId
                })
                .ToListAsync();

            var viewModel = new ExamBankDetailVM
            {
                ExamBankId = id,
                ExamBankName = examBankInfo!.ExamBankName,
                CreatedDate = examBankInfo!.CreatedDate,
                ExamType = examBankInfo!.ExamType,
                Subject = monHoc!,
                ExamBankQuestions = examQuestions
            };

            return View(viewModel);
        }


        //Ngân hàng đề : Tạo một ngân hàng đề thi - OK
        [HttpGet]
        public async Task<IActionResult> Exam_Bank_Create()
        {
            string access_token = Request.Cookies["access_token"]!;
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Không thể lấy danh sách môn học.");
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root!["data"];

            var subjectList = dataToken!.ToObject<List<SubjectDto>>();
            ViewBag.Subjects = subjectList!.Select(s => new CreateExamBankVM
            {
                SubjectId = s.Id,
                SubjectCode = s.Code,
                SubjectName = s.Name
            }).ToList();
            return View();
        }


        //Ngân hàng đề : Chỉnh sửa ngân hàng đề - OK
        [HttpPost]
        public async Task<IActionResult> EditExamBank(int MaNHD, string TenNHD, bool TrangThai)
        {
            var examBank = await _context.ExamBanks.FirstOrDefaultAsync(nhd => nhd.ExamBankId == MaNHD);
            if (examBank == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            examBank.ExamBankName = TenNHD;
            examBank.Status = TrangThai;

            _context.ExamBanks.Update(examBank);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật ngân hàng đề thành công.";

            if (examBank.ExamType == "Tự luận")
            {
                return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = MaNHD });
            }
            else
            {
                return RedirectToAction("Exam_Bank_Add_Question_Multiplechoice", new { id = MaNHD });
            }
        }


        //Ngân hàng đề : Lưu một ngân hàng đề đã tạo - OK
        [HttpPost]
        public async Task<IActionResult> Exam_Bank_Create(CreateExamBankVM model)
        {
            // Kiểm tra tài khoản giảng viên
            var accessToken = Request.Cookies["access_token"];
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.AccessToken == accessToken)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
            if (userId == 0)
                return RedirectToAction("Logout", "Auth");

            var newExamBank = new ExamBank
            {
                ExamBankName = model.ExamBankName,
                CreatedDate = DateOnly.FromDateTime(DateTime.Now).ToDateTime(TimeOnly.MinValue),
                Status = model.Status,
                SubjectId = model.SubjectId,
                ExamType = model.ExamType,
                UserId = userId
            };

            _context.Add(newExamBank);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tạo ngân hàng đề thành công!";

            if (newExamBank.ExamType == "Trắc nghiệm")
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = newExamBank.ExamBankId });
            else
                return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = newExamBank.ExamBankId });
        }


        //Ngân hàng đề : Câu hỏi : Thêm câu hỏi trắc nghiệm - OK-
        public async Task<IActionResult> Exam_Bank_Add_Question_MultipleChoice(int id)
        {
            var examBank = await _context.ExamBanks
                .AsNoTracking()
                .Where(e => e.ExamBankId == id)
                .Select(e => new
                {
                    e.SubjectId,
                    e.ExamBankName,
                    e.Status
                }).FirstOrDefaultAsync();
            if (examBank == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            int subjectId = examBank.SubjectId;

            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Không thể lấy dữ liệu môn học.");
            }
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root!["data"];
            var subjectList = dataToken!.ToObject<List<SubjectDto>>();

            var subject = subjectList!.FirstOrDefault(s => s.Id == subjectId);
            if (subject == null)
                return NotFound("Không tìm thấy môn học.");

            var chapters = subject.Chapters ?? new List<ChapterDto>();

            var questions = await _context.QuestionBanks
                .AsNoTracking()
                .Where(q => q.ExamBankId == id)
                .ToListAsync();

            var questionDTO = questions.Select(ch => new QuestionDto
            {
                QuestionId = ch.QuestionId,
                QuestionContent = ch.QuestionContent,
                ChoiceContent = ch.ChoiceContent!,
                QuestionType = ch.QuestionType,
                CloId = ch.CLOId ?? 0,
                ChapterId = ch.ChapterId ?? 0
            }).ToList();

            var viewModel = new CreateQuestionVM
            {
                ExamBankId = id,
                ExamBankName = examBank.ExamBankName,
                IsActive = examBank.Status,
                QuestionList = questions,
                QuestionDTOList = questionDTO,
                ChapterList = subject?.Chapters ?? new List<ChapterDto>()
            };

            return View(viewModel);
        }


        //Ngân hàng đề : Câu hỏi : Lưu câu hỏi trắc nghiệm
        [HttpPost]
        public async Task<IActionResult> Exam_Bank_Add_Question_MultipleChoice(CreateQuestionVM model)
        {
            string errMessage = "";

            if (string.IsNullOrWhiteSpace(model.QuestionContent))
            {
                errMessage = "Nội dung câu hỏi không được bỏ trống.";
            }
            if (!string.IsNullOrEmpty(errMessage))
            {
                TempData["ErrorMessage"] = errMessage;
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = model.ExamBankId });
            }

            if (model.ChoiceContents == null || !model.ChoiceContents.Any() || model.ChoiceContents.All(string.IsNullOrWhiteSpace))
            {
                errMessage = "Nội dung lựa chọn không được bỏ trống.";
            }
            if (!string.IsNullOrEmpty(errMessage))
            {
                TempData["ErrorMessage"] = errMessage;
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = model.ExamBankId });
            }

            bool hasTrueAnswer = model.CorrectAnswers != null && model.CorrectAnswers.Any(laDapAn => laDapAn == "true");
            if (!hasTrueAnswer)
            {
                errMessage = "Bạn cần chọn ít nhất một đáp án đúng.";
            }
            if (!string.IsNullOrEmpty(errMessage))
            {
                TempData["ErrorMessage"] = errMessage;
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = model.ExamBankId });
            }

            var examBank = await _context.ExamBanks.FindAsync(model.ExamBankId);
            if (examBank == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            int subjectId = examBank.SubjectId;

            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Không thể lấy dữ liệu môn học.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var subjectList = root!["data"]!.ToObject<List<SubjectDto>>();
            var subject = subjectList!.FirstOrDefault(s => s.Id == subjectId);
            model.ChapterList = subject?.Chapters ?? new List<ChapterDto>();

            var luaChonList = new List<object>();
            for (int i = 0; i < model.ChoiceContents!.Count; i++)
            {
                bool laDapAn = model.CorrectAnswers != null && model.CorrectAnswers.Count > i && model.CorrectAnswers[i] == "true";
                luaChonList.Add(new
                {
                    Id = i + 1,
                    NoiDung = model.ChoiceContents[i],
                    LaDapAn = laDapAn
                });
            }
            string luaChonJson = JsonConvert.SerializeObject(luaChonList);

            if (model.QuestionId > 0)
            {
                var cauHoi = await _context.QuestionBanks.FirstOrDefaultAsync(ch => ch.QuestionId == model.QuestionId);
                if (cauHoi == null)
                {
                    TempData["ErrorMessage"] = "Lỗi: Câu hỏi không tồn tại.";
                    return View(model);
                }

                cauHoi.QuestionContent = model.QuestionContent;
                cauHoi.QuestionType = model.QuestionType;
                cauHoi.CLOId = model.CLOId > 0 ? model.CLOId : null;
                cauHoi.ChapterId = model.ChapterId > 0 ? model.ChapterId : null;
                cauHoi.ChoiceContent = luaChonJson;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Câu hỏi đã được cập nhật thành công.";
            }
            else
            {
                var cauHoi = new QuestionBank
                {
                    QuestionContent = model.QuestionContent,
                    ExamBankId = model.ExamBankId,
                    QuestionType = model.QuestionType,
                    CLOId = model.CLOId > 0 ? model.CLOId : null,
                    ChapterId = model.ChapterId > 0 ? model.ChapterId : null,
                    ChoiceContent = luaChonJson
                };

                _context.QuestionBanks.Add(cauHoi);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Câu hỏi và đáp án đã được thêm thành công.";
            }

            return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = model.ExamBankId });
        }

        // Ngân hàng đề : Câu hỏi : Import câu hỏi từ file Word (Đọc định dạng + Tra cứu ID + Xác định QuestionType)
        [HttpPost]
        public async Task<IActionResult> Exam_Bank_Import_From_Word_Formatted(IFormFile wordFile, int examBankId)
        {
            if (wordFile == null || wordFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn một file Word để tải lên.";
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = examBankId });
            }
            if (!Path.GetExtension(wordFile.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ chấp nhận file .docx.";
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = examBankId });
            }

            var questionsToAdd = new List<QuestionBank>();
            var errors = new List<string>();

            var examBank = await _context.ExamBanks.FindAsync(examBankId);
            if (examBank == null) return NotFound("Không tìm thấy ngân hàng đề.");

            List<ChapterDto> chapterList = new List<ChapterDto>();
            List<CloDto> allClosList = new List<CloDto>();

            try
            {
                int subjectId = examBank.SubjectId;
                var access_token = Request.Cookies["access_token"];
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
                var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Lỗi API: Không thể lấy dữ liệu môn học.";
                    return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = examBankId });
                }

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonConvert.DeserializeObject<JObject>(json);
                var subjectList = root!["data"]!.ToObject<List<SubjectDto>>();
                var subject = subjectList!.FirstOrDefault(s => s.Id == subjectId);

                if (subject == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy môn học.";
                    return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = examBankId });
                }

                chapterList = subject.Chapters ?? new List<ChapterDto>();
                allClosList = chapterList.SelectMany(c => c.Clos).ToList();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi lấy dữ liệu: " + ex.Message;
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = examBankId });
            }

            string currentChapterName = null; 
            string currentCloName = null;  

            try
            {
                using (var stream = wordFile.OpenReadStream())
                {
                    var document = new XWPFDocument(stream);
                    var paragraphs = document.Paragraphs;

                    QuestionBank currentQuestion = null;
                    List<dynamic> currentChoices = new List<dynamic>();

                    var regexChapter = new Regex(@"^Chương:\s*(.+)", RegexOptions.IgnoreCase);
                    var regexClo = new Regex(@"^CLO:\s*(.+)", RegexOptions.IgnoreCase);
                    var regexQuestion = new Regex(@"^Câu\s*\d+[:.]\s*(.*)", RegexOptions.IgnoreCase);
                    var regexChoice = new Regex(@"^([A-Z])\.\s*(.*)", RegexOptions.IgnoreCase);

                    foreach (var para in paragraphs)
                    {
                        string text = para.ParagraphText.Trim();

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            if (currentQuestion != null)
                            {
                                ProcessAndAddQuestion_Formatted(questionsToAdd, errors, ref currentQuestion, ref currentChoices);
                            }
                            continue;
                        }

                        var matchChapter = regexChapter.Match(text);
                        var matchClo = regexClo.Match(text);
                        var matchQuestion = regexQuestion.Match(text);
                        var matchChoice = regexChoice.Match(text);

                        if (matchChapter.Success)
                        {
                            currentChapterName = matchChapter.Groups[1].Value.Trim();
                            currentCloName = null;
                        }
                        else if (matchClo.Success)
                        {
                            currentCloName = matchClo.Groups[1].Value.Trim();
                        }
                        else if (matchQuestion.Success)
                        {
                            if (currentQuestion != null)
                            {
                                ProcessAndAddQuestion_Formatted(questionsToAdd, errors, ref currentQuestion, ref currentChoices);
                            }

                            int? foundChapterId = null;
                            int? foundCloId = null;
                            var questionContent = matchQuestion.Groups[1].Value.Trim();
                            var questionIdentifier = $"Câu \"{questionContent.Substring(0, Math.Min(questionContent.Length, 30))}...\"";

                            if (!string.IsNullOrEmpty(currentChapterName))
                            {
                                var chapter = chapterList.FirstOrDefault(c => c.Title.Trim().Equals(currentChapterName, StringComparison.OrdinalIgnoreCase));
                                if (chapter != null) foundChapterId = chapter.Id;
                                else errors.Add($"{questionIdentifier}: Không tìm thấy Chương '{currentChapterName}' trong hệ thống.");
                            }

                            if (!string.IsNullOrEmpty(currentCloName))
                            {
                                var clo = allClosList.FirstOrDefault(c => c.Title.Trim().StartsWith(currentCloName, StringComparison.OrdinalIgnoreCase));
                                if (clo != null) foundCloId = clo.Id;
                                else errors.Add($"{questionIdentifier}: Không tìm thấy CLO '{currentCloName}' trong hệ thống.");
                            }

                            currentQuestion = new QuestionBank
                            {
                                QuestionContent = questionContent,
                                ExamBankId = examBankId,
                                ChapterId = foundChapterId,
                                CLOId = foundCloId
                            };
                            currentChoices = new List<dynamic>();

                        }
                        else if (currentQuestion != null && matchChoice.Success)
                        {
                            bool isCorrect = CheckParagraphFormatting(para);
                            currentChoices.Add(new
                            {
                                Id = matchChoice.Groups[1].Value.ToUpper(),
                                NoiDung = matchChoice.Groups[2].Value.Trim(),
                                LaDapAn = isCorrect
                            });
                        }
                    }
                    if (currentQuestion != null)
                    {
                        ProcessAndAddQuestion_Formatted(questionsToAdd, errors, ref currentQuestion, ref currentChoices);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add("Lỗi đọc file: " + ex.Message);
            }

            if (errors.Any())
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine($"Có {errors.Count} lỗi. Không dữ liệu nào được lưu:");
                foreach (var err in errors.Take(10)) errorBuilder.AppendLine($"- {err}");
                TempData["ErrorMessage"] = errorBuilder.ToString();
            }
            else if (questionsToAdd.Any())
            {
                _context.QuestionBanks.AddRange(questionsToAdd);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã thêm {questionsToAdd.Count} câu hỏi thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "File không có dữ liệu câu hỏi hợp lệ.";
            }

            return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = examBankId });
        }

        // (Hàm hỗ trợ 1) Kiểm tra định dạng (In đậm, Gạch chân, Màu) của một Paragraph.
        private bool CheckParagraphFormatting(XWPFParagraph para)
        {
            foreach (var run in para.Runs)
            {
                if (run.IsBold)
                    return true;

                if (run.Underline != UnderlinePatterns.None)
                    return true;

                string color = run.GetColor();
                if (!string.IsNullOrEmpty(color) &&
                    !color.Equals("000000", StringComparison.OrdinalIgnoreCase) &&
                    !color.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // (Hàm hỗ trợ 2) Xử lý, xác thực, gán QuestionType và thêm câu hỏi vào danh sách.
        private void ProcessAndAddQuestion_Formatted(List<QuestionBank> questionsToAdd, List<string> errors,
                                             ref QuestionBank currentQuestion, ref List<dynamic> currentChoices)
        {
            if (currentQuestion == null)
            {
                return;
            }

            string questionIdentifier = $"Câu \"{currentQuestion.QuestionContent.Substring(0, Math.Min(currentQuestion.QuestionContent.Length, 40))}...\"";

            if (currentChoices == null || !currentChoices.Any())
            {
                errors.Add($"{questionIdentifier}: Không có lựa chọn nào (A, B, C...).");
                currentQuestion = null; 
                return;
            }

            int correctAnwerCount = currentChoices.Count(c => c.LaDapAn == true);
            int totalChoiceCount = currentChoices.Count;

            if (correctAnwerCount == 0)
            {
                errors.Add($"{questionIdentifier}: Không tìm thấy đáp án đúng nào (thiếu in đậm/gạch chân/màu).");
                currentQuestion = null; 
                return;
            }

            if (correctAnwerCount > 1)
            {
                currentQuestion.QuestionType = "2"; 
            }
            else if (correctAnwerCount == 1)
            {
                if (totalChoiceCount == 2)
                {
                    currentQuestion.QuestionType = "3"; 
                }
                else
                {
                    currentQuestion.QuestionType = "1"; 
                }
            }

            var finalChoices = new List<object>();
            int choiceIdCounter = 1;

            foreach (var choice in currentChoices)
            {
                finalChoices.Add(new
                {
                    Id = choiceIdCounter++, 
                    NoiDung = choice.NoiDung,
                    LaDapAn = choice.LaDapAn
                });
            }

            currentQuestion.ChoiceContent = JsonConvert.SerializeObject(finalChoices);
            questionsToAdd.Add(currentQuestion);

            currentQuestion = null;
            currentChoices = null;
        }


        // Hàm trợ giúp để xử lý và xác thực câu hỏi trước khi thêm vào danh sách
        private void ProcessAndAddQuestion(List<QuestionBank> questionsToAdd, List<string> errors,
                                         ref QuestionBank currentQuestion, ref List<dynamic> currentChoices,
                                         ref string correctAnswerLetter, int examBankId)
        {
            string questionIdentifier = $"Câu \"{currentQuestion.QuestionContent.Substring(0, Math.Min(currentQuestion.QuestionContent.Length, 30))}...\"";

            if (currentChoices == null || !currentChoices.Any())
            {
                errors.Add($"{questionIdentifier}: Không có lựa chọn nào (A, B, C, D...).");
                currentQuestion = null; 
                return;
            }

            if (string.IsNullOrWhiteSpace(correctAnswerLetter))
            {
                errors.Add($"{questionIdentifier}: Không có dòng 'Đáp án: [chữ cái]'.");
                currentQuestion = null;
                return;
            }

            bool foundCorrectAnswer = false;
            var finalChoices = new List<object>();
            int choiceIdCounter = 1;

            foreach (var choice in currentChoices)
            {
                bool isCorrect = choice.Id == correctAnswerLetter;
                if (isCorrect)
                {
                    foundCorrectAnswer = true;
                }
                finalChoices.Add(new
                {
                    Id = choiceIdCounter++, 
                    NoiDung = choice.NoiDung,
                    LaDapAn = isCorrect
                });
            }

            if (!foundCorrectAnswer)
            {
                errors.Add($"{questionIdentifier}: Dòng 'Đáp án: {correctAnswerLetter}' không khớp với bất kỳ lựa chọn nào.");
                currentQuestion = null;
                return;
            }

            currentQuestion.ChoiceContent = JsonConvert.SerializeObject(finalChoices);
            questionsToAdd.Add(currentQuestion);

            currentQuestion = null;
            currentChoices = null;
            correctAnswerLetter = null;
        }

        //Ngân hàng đề : Câu hỏi : Thêm câu hỏi tự luận
        public async Task<IActionResult> Exam_Bank_Add_Question_Essay(int id)
        {
            var examBank = await _context.ExamBanks.AsNoTracking().Where(e => e.ExamBankId == id)
                .Select(e => new
                {
                    e.ExamBankName,
                    e.Status,
                    e.SubjectId
                }).FirstOrDefaultAsync();

            if (examBank == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            int subjectId = examBank.SubjectId;

            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Không thể lấy dữ liệu môn học.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root!["data"];
            var subjectList = dataToken!.ToObject<List<SubjectDto>>();

            var subject = subjectList!.FirstOrDefault(s => s.Id == subjectId);
            if (subject == null)
                return NotFound("Không tìm thấy môn học.");

            var chapters = subject.Chapters ?? new List<ChapterDto>();

            var questions = await _context.QuestionBanks.AsNoTracking()
                .Where(ch => ch.ExamBankId == id)
                .ToListAsync();

            var questionDTOs = questions.Select(ch => new QuestionDto
            {
                QuestionId = ch.QuestionId,
                QuestionContent = ch.QuestionContent,
                CloId = ch.CLOId ?? 0,
                ChapterId = ch.ChapterId ?? 0

            }).ToList();

            var viewModel = new CreateQuestionVM
            {
                ExamBankId = id,
                ExamBankName = examBank.ExamBankName,
                IsActive = examBank.Status,
                QuestionList = questions,
                QuestionDTOList = questionDTOs,
                ChapterList = chapters
            };
            return View(viewModel);
        }


        //Ngân hàng đề : Câu hỏi : Lưu câu hỏi tự luận
        [HttpPost]
        public async Task<IActionResult> Exam_Bank_Add_Question_Essay(CreateQuestionVM model)
        {
            string errMessage = "";

            if (string.IsNullOrWhiteSpace(model.QuestionContent))
            {
                errMessage = "Nội dung câu hỏi không được bỏ trống.";
            }

            if (!string.IsNullOrEmpty(errMessage))
            {
                TempData["ErrorMessage"] = errMessage;
                return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = model.ExamBankId });
            }

            var examBank = await _context.ExamBanks
                .FindAsync(model.ExamBankId);
            if (examBank == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            int subjectId = examBank.SubjectId;
            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Không thể lấy dữ liệu môn học.");
            }
            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var subjectList = root!["data"]!.ToObject<List<SubjectDto>>();
            var subject = subjectList!.FirstOrDefault(s => s.Id == subjectId);

            model.ChapterList = subject?.Chapters ?? new List<ChapterDto>();

            if (model.QuestionId > 0)
            {
                var question = await _context.QuestionBanks.FirstOrDefaultAsync(q => q.QuestionId == model.QuestionId);
                if (question == null)
                {
                    TempData["ErrorMessage"] = "Lỗi: Câu hỏi không tồn tại.";
                    return View(model);
                }

                question.QuestionContent = model.QuestionContent;
                question.CLOId = model.CLOId > 0 ? model.CLOId : null;
                question.ChapterId = model.ChapterId > 0 ? model.ChapterId : null;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Câu hỏi đã được cập nhật thành công.";
            }
            else
            {
                var question = new QuestionBank
                {
                    QuestionContent = model.QuestionContent,
                    ExamBankId = model.ExamBankId,
                    CLOId = model.CLOId > 0 ? model.CLOId : null,
                    ChapterId = model.ChapterId > 0 ? model.ChapterId : null,
                };

                _context.QuestionBanks.Add(question);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Câu hỏi đã được thêm thành công.";
            }

            return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = model.ExamBankId });
        }


        //Ngân hàng đề : Câu hỏi : Xóa câu hỏi
        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int maCauHoi, int MaNHD)
        {
            var cauHoi = await _context.QuestionBanks.FirstOrDefaultAsync(ch => ch.QuestionId == maCauHoi);
            var nganHangDe = await _context.ExamBanks.FirstOrDefaultAsync(nhd => nhd.ExamBankId == MaNHD);

            if (cauHoi == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy câu hỏi.";
                if (nganHangDe?.ExamType == "Tự luận")
                {
                    return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = MaNHD });
                }
                else
                {
                    return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = MaNHD });
                }
            }

            var isRelatedToAnswers = await _context.StudentAnswers.AnyAsync(sa => sa.QuestionId == maCauHoi);

            string relatedInfo = "";
            if (isRelatedToAnswers)
            {
                relatedInfo += "Câu hỏi này có liên quan đến một số câu trả lời của sinh viên.\n";
            }

            if (!string.IsNullOrEmpty(relatedInfo))
            {
                TempData["ErrorMessage"] = $"Không thể xóa câu hỏi:\n{relatedInfo}";
                if (nganHangDe?.ExamType == "Tự luận")
                {
                    return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = MaNHD });
                }
                else
                {
                    return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = MaNHD });
                }
            }

            _context.QuestionBanks.Remove(cauHoi);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Câu hỏi đã được xóa thành công.";

            if (nganHangDe?.ExamType == "Tự luận")
            {
                return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = MaNHD });
            }
            else
            {
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = MaNHD });
            }
        }


        //Ngân hàng đề : Xóa ngân hàng đề 
        [HttpPost]
        public async Task<IActionResult> XoaNganHangDe(int maNHD)
        {
            var nganHangDe = await _context.ExamBanks
                .Include(nhd => nhd.Exams)
                .Include(nhd => nhd.QuestionBanks)
                .FirstOrDefaultAsync(nhd => nhd.ExamBankId == maNHD);
            if (nganHangDe == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            if (nganHangDe.Exams != null)
            {
                foreach (var deThi in nganHangDe.Exams)
                {
                    if (deThi.ExamHistories != null)
                        _context.ExamHistories.RemoveRange(deThi.ExamHistories);

                    if (deThi.ExamMatrices != null)
                        _context.ExamMatrices.RemoveRange(deThi.ExamMatrices);

                    if (deThi.ClassExams != null)
                        _context.ClassExams.RemoveRange(deThi.ClassExams);

                    _context.Exams.Remove(deThi);
                }
            }

            if (nganHangDe.QuestionBanks != null)
            {
                foreach (var cauHoi in nganHangDe.QuestionBanks)
                {
                    if (cauHoi.StudentAnswers != null)
                        _context.StudentAnswers.RemoveRange(cauHoi.StudentAnswers);
                    _context.QuestionBanks.Remove(cauHoi);
                }
            }

            _context.ExamBanks.Remove(nganHangDe);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa ngân hàng đề thành công!";
            return RedirectToAction("ExamBank");
        }


        //Ngân hàng đề : Lấy đề thi theo ngân hàng đề - OK
        [HttpGet] //API
        public async Task<IActionResult> GetExamsByExamBank(int maNHD)
        {
            var exams = await _context.Exams
                .AsNoTracking()
                .Where(e => e.ExamBankId == maNHD)
                .Select(e => new {
                    e.ExamId,
                    e.ExamName,
                    e.CreatedDate
                })
                .ToListAsync();
            return Ok(exams);
        }

        #endregion

        //================================================================

        #region Monitoring
        public async Task<IActionResult> Monitoring(int examId, int classId)
        {
            var access_token = Request.Cookies["access_token"];
            var user = _context.Users.AsNoTracking().Where(us => us.AccessToken == access_token)
                .Select(us => new { us.Role, us.FullName }).FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            if (user.Role?.ToLower() != "teacher")
            {
                return NotFound();
            }

            var classInfo = await _context.Classes
                .AsNoTracking()
                .Where(c => c.ClassId == classId)
                .Select(c => new
                {
                    c.ClassId,
                    c.ClassName,
                    Students = c.ClassDetails
                        .Where(cd => cd.User != null)
                        .Select(cd => new StudentVM
                        {
                            UserId = cd.UserId,
                            FullName = cd.User.FullName,
                            Username = cd.User.Username
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (classInfo == null)
            {
                return NotFound();
            }

            var Exam = _context.Exams.SingleOrDefault(dt => dt.ExamId == examId);
            if (Exam == null)
            {
                return NotFound();
            }

            ViewBag.Instructor = user.FullName;

            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.ExamId == examId)
                .Select(e => new
                {
                    e.ExamId,
                    e.ExamName,
                    e.ExamType,
                    e.Duration
                })
                .FirstOrDefaultAsync();

            if (exam == null)
            {
                return NotFound();
            }

            var viewModel = new MonitoringVM
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                Duration = exam.Duration,
                ClassId = classInfo.ClassId,
                ClassName = classInfo.ClassName,
                StudentList = classInfo.Students
            };

            return View(viewModel);
        }

        #endregion

        //================================================================

        #region ExamLogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLogs([FromBody] DeleteLogsDto req)
        {
            if (req == null || req.LogIds == null || req.LogIds.Count == 0)
                return Json(new { success = false, message = "Không có bản ghi nào được chọn." });

            try
            {
                // Xóa an toàn theo ClassId + danh sách LogIds
                var toDelete = await _context.ActivityLogs
                    .Where(x => req.LogIds.Contains(x.ActivityLogId) && x.ClassId == req.ClassId)
                    .Select(x => x.ActivityLogId)
                    .ToListAsync();

                if (toDelete.Count == 0)
                    return Json(new { success = false, message = "Không tìm thấy bản ghi phù hợp để xóa." });

                // Load entities để RemoveRange
                var entities = await _context.ActivityLogs
                    .Where(x => toDelete.Contains(x.ActivityLogId))
                    .ToListAsync();

                _context.ActivityLogs.RemoveRange(entities);
                await _context.SaveChangesAsync();

                return Json(new { success = true, deleted = entities.Count, deletedIds = toDelete });
            }
            catch (Exception ex)
            {
                // log ex
                return Json(new { success = false, message = "Lỗi máy chủ: " + ex.Message });
            }
        }
        #endregion
    }
}