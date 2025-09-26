using Azure.Core;
using Hangfire;
using ITExam.ExternalModels;
using ITExam.ExternalModels.Subject;
using ITExam.Filters;
using ITExam.Models;
using ITExam.Services;
using ITExam.ViewModels;
using ITExam.ViewModels.Class;
using ITExam.ViewModels.Exam;
using ITExam.ViewModels.ExamBank;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using WebApplication1.Models;
using ChapterCLOVM = ITExam.ViewModels.Exam.ChapterCLOVM;
using CLOItem = ITExam.ViewModels.Exam.CLOItem;

namespace ITExam.Controllers
{
    //[CheckToken]
    [AuthorizeRole("teacher")]
    public class TeacherController : Controller
    {
        private readonly ITExamDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IClassCodeService _classCodeService;
        public TeacherController(IHttpClientFactory httpClientFactory, ITExamDbContext context, IClassCodeService classCodeService)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _classCodeService = classCodeService;
        }

        //================================================================

        #region LopHoc
        //Lớp học : Danh sách lớp học -
        public async Task<IActionResult> Class()
        {
            // Check tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(us => us.AccessToken == access_token);

            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var viewModel = await _context.Classes
                .AsNoTracking()
                .Where(cl => cl.UserId == user.UserId)
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


        //Lớp học : Chỉnh sửa lớp học -
        [HttpPost]
        public async Task<IActionResult> EditClass(ClassVM model)
        {
            //Truy vấn lớp học để chỉnh sửa
            var classEdit = await _context.Classes.FindAsync(model.ClassId);
            if (classEdit == null)
            {
                return NotFound();
            }

            //Cập nhật tên lớp học và mô tả
            classEdit.ClassName = model.ClassName;
            classEdit.Description = model.Description;

            _context.Update(classEdit); //Sua sau
            await _context.SaveChangesAsync();

            return RedirectToAction("Class");
        }


        //Lớp học : Thêm lớp học -
        [HttpPost]
        public async Task<IActionResult> Class(Class model)
        {
            //Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            //Set ngày tạo và người tạo lớp học
            model.CreatedDate = DateTime.Now;
            model.UserId = user.UserId;
            model.ClassCode = await _classCodeService.GenerateUniqueClassCodeAsync();

            _context.Classes.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm lớp học thành công!";
            return RedirectToAction("Class");
        }


        //Lớp học : Chi tiết lớp học -
        public async Task<IActionResult> ClassDetail(int id)
        {
            // Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
                return RedirectToAction("Logout", "Auth");

            // Truy vấn lớp học theo Id
            var thisClass = await _context.Classes
                .Include(l => l.ClassDetails)
                    .ThenInclude(ct => ct.User)
                .Include(l => l.ClassExams)
                    .ThenInclude(dt => dt.Exam)
                        .ThenInclude(d => d.ExamMatrices)
                .Include(l => l.ClassExams)
                    .ThenInclude(dt => dt.Exam)
                        .ThenInclude(d => d.ExamHistories)
                .FirstOrDefaultAsync(l => l.ClassId == id);

            if (thisClass == null)
                return NotFound();

            // Danh sách sinh viên
            var danhSachSV = thisClass.ClassDetails
                .Where(ct => ct.User != null)
                .Select(ct => new ClassStudentVM
                {
                    StudentId = ct.User.Username,
                    FullName = ct.User.FullName,
                    Email = ct.User.Email,
                    JoinDate = ct.JoinDate
                }).ToList();

            // Danh sách đề thi đã thêm vào lớp
            var danhSachDT = thisClass.ClassExams
                .Where(dt => dt.Exam != null)
                .Select(dt => new ClassExamVM
                {
                    ExamId = dt.Exam.ExamId,
                    ExamName = dt.Exam.ExamName,
                    Type = dt.IsExam,  //Sua sau
                    CreatedDate = dt.AddedDate,
                    ExamType = dt.Exam.ExamType,
                    StartDate = dt.StartTime,
                    EndDate = dt.EndTime,
                    Duration = dt.Exam.Duration,
                    QuestionCount = dt.Exam.ExamMatrices?.Sum(m => m.QuestionCount) ?? 0,
                    Access = dt.Access ?? false,
                    UngradedStudentCount =
                        dt.Exam.ExamType?.ToLower() == "tự luận" && dt.Exam.ExamHistories != null
                        ? dt.Exam.ExamHistories
                            .Count(ls => ls.ClassId == id && ls.SubmitTime != null && ls.Score == null)
                        : 0
                }).ToList();

            // Danh sách đề thi chưa thêm vào lớp
            var danhSachDeThiChuaThem = await _context.Exams
                 .Where(dt => !_context.ClassExams.Any(ct => ct.ExamId == dt.ExamId && ct.ClassId == id)
                              && dt.UserId == user.UserId)
                 .Select(dt => new UnassignedExamVM
                 {
                     InstructorId = dt.UserId,
                     ExamId = dt.ExamId,
                     ExamName = dt.ExamName,
                     Duration = dt.Duration,
                     CreatedDate = dt.CreatedDate,
                     QuestionCount = dt.ExamMatrices.Sum(m => m.QuestionCount)
                 }).ToListAsync();


            var logs = await _context.ActivityLogs
                .Where(lop => lop.ClassId == id)
                .Include(x => x.User)
                .Include(x => x.Exam)
                .Include(x => x.Class)
                .OrderByDescending(x => x.LogDate)
                .Select(log => new ExamLogEntryVM
                {
                    LogId = log.ActivityLogId,
                    StudentFullName = log.User.FullName,
                    InstructorName = log.InstructorName, //TeacherName
                    ExamName = log.Exam.ExamName,
                    ClassName = log.Class.ClassName,
                    LoggedDate= log.LogDate,
                    LogContent = log.LogContent
                })
                .ToListAsync();

            var viewModel = new ClassDetailVM
            {
                ClassInfo = thisClass,
                Students = danhSachSV,
                Logs = logs,
                Exams = danhSachDT,
                UnassignedExams = danhSachDeThiChuaThem,
                StudentCount = danhSachSV.Count,
                ExamCount = danhSachDT.Count(dt => dt.Type),
                ClassCode = thisClass.ClassCode
            };

            return View(viewModel);
        }


        //Lớp học : Danh sách sinh viên đã làm bài
        public async Task<IActionResult> List_Student_Do_Test(int id, int maLopHoc)
        {
            // Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // Truy vấn lớp học theo mã lớp học (maLopHoc)
            var thisClass = await _context.Classes
            .Include(l => l.ClassDetails) // Join bảng ChiTietLopHoc để lấy danh sách sinh viên
            .ThenInclude(st => st.User) // Join bảng User để lấy thông tin sinh viên
            .Include(ct => ct.ClassExams)  // Join bảng DanhSachDeThiCuaLopHoc để lấy danh sách đề thi
            .ThenInclude(dt => dt.Exam)
            .ThenInclude(ls => ls.ExamHistories) // Join bảng LichSuLamBai để kiểm tra sinh viên nào đã làm bài
            .FirstOrDefaultAsync(l => l.ClassId == maLopHoc); // Truy vấn lớp học bằng maLopHoc

            if (thisClass == null || thisClass.ClassDetails == null || thisClass.ClassExams == null)
                return NotFound();

            // Kiểm tra xem đề thi có tồn tại trong lớp học không bằng cách kiểm tra MaDe và MaLopHoc
            var isExamInClass = thisClass.ClassExams
                .Any(deThi => deThi.ClassId == maLopHoc && deThi.ExamId == id); // Kiểm tra cả MaLopHoc và MaDe

            if (!isExamInClass)
            {
                return NotFound("Đề thi không tồn tại trong lớp học này.");
            }

            var danhSachSV = thisClass.ClassDetails
            .Select(l => l.User)
            .Where(sv => sv != null)
            .Select(sv =>
            {
                // Tìm bản ghi LichSuLamBai tương ứng với sinh viên, đề thi và lớp học
                var lichSu = thisClass.ClassExams
                    .SelectMany(d => d.Exam.ExamHistories)
                    .FirstOrDefault(ls => ls.UserId == sv.UserId && ls.ExamId == id && ls.ClassId == maLopHoc);

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
            .Where(vm => vm != null) // Bỏ qua sinh viên chưa làm bài
            .ToList();

            var viewModel = new StudentSubmissionList
            {
                ClassInfo = thisClass,
                Students = danhSachSV
            };

            return View(viewModel);
        }

        #endregion

        //================================================================

        #region DeThi
        //Đề thi : Danh sách đề thi
        public async Task<IActionResult> Exam()
        {
            //Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.SingleOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }
            //============
            var deThis = _context.Exams.Include(d => d.ExamBank).Include(d => d.User)
                .Include(mt => mt.ExamMatrices)
                .Where(dt => dt.UserId == user.UserId).ToList();
            return View(deThis);
        }


        //Đề thi : Tạo đề thi từ ngân hàng đề
        public async Task<IActionResult> Exam_Create()
        {
            //Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.SingleOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // Lấy danh sách ngân hàng đề, phân loại theo trạng thái
            var nganHangDeListCuaToi = await _context.ExamBanks.Where(n => n.UserId == user.UserId).ToListAsync();
            var nganHangDeListCongKhai = await _context.ExamBanks.Where(n => n.Status == true).ToListAsync();

            // Truyền vào ViewData
            ViewData["NganHangDeListCuaToi"] = nganHangDeListCuaToi;
            ViewData["NganHangDeListCongKhai"] = nganHangDeListCongKhai;

            return View();
        }


        //Đề thi : Lưu đề thi đã tạo từ ngân hàng đề
        [HttpPost]
        public async Task<IActionResult> Exam_Create(Exam deThiMoi)
        {
            //Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            var nganHangDe = await _context.ExamBanks.FirstOrDefaultAsync(n => n.ExamBankId == deThiMoi.ExamBankId);
            if (nganHangDe == null)
            {

                return RedirectToAction("Error", "Home");
            }
            deThiMoi.UserId = user.UserId;
            deThiMoi.CreatedDate = DateTime.Now;
            deThiMoi.ExamType = nganHangDe.ExamType;
            _context.Add(deThiMoi);
            // Replace this line:
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã tạo đề thi thành công!";
            return RedirectToAction("Exam_Create_Matrix", new { id = deThiMoi.ExamId });
        }


        //Đề thi : Tạo ma trận đề thi, lấy câu hỏi từ ngân hàng đề
        public async Task<IActionResult> Exam_Create_Matrix(int id)
        {
            // Lấy đề thi & ngân hàng đề
            var deThi = await _context.Exams
                                .Include(d => d.ExamBank)
                                .ThenInclude(n => n.QuestionBanks)
                                .FirstOrDefaultAsync(d => d.ExamId == id);

            if (deThi == null)
            {
                return NotFound();
            }

            // Lấy môn học
            var maMonHoc = deThi.ExamBank.SubjectId;
            var access_token = Request.Cookies["access_token"];

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Không thể lấy danh sách môn học.");

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root["data"];
            var subjectList = dataToken.ToObject<List<SubjectDto>>();
            var subject = subjectList.FirstOrDefault(s => s.Id == maMonHoc);
            var subjectName = subject?.Name;

            var cauHoiTrongNganHang = deThi.ExamBank.QuestionBanks;

            var chuongList = subject?.Chapters.Select(c => new
            {
                ChuongId = c.Id,
                TenChuong = c.Title,
                Clos = c.Clos.Select(clo => new
                {
                    CLOId = clo.Id,
                    CLOTitle = clo.Title,
                    CLODescription = clo.Description,
                    MaxQuestionCount = cauHoiTrongNganHang
                                    .Count(ch => ch.ChapterId == c.Id && ch.CLOId == clo.Id)
                }).ToList()
            }).ToList();

            var maTranList = await _context.ExamMatrices
                                           .Where(m => m.ExamId == id)
                                           .ToListAsync();

            // Chuẩn bị danh sách chương hiển thị trong viewmodel
            var danhSachChuong = subject?.Chapters.Select(ch => new ChapterCLOVM
            {
                ChapterId = ch.Id,
                ChapterTitle = ch.Title,
                CLOs = ch.Clos.Select(clo => new CLOItem
                {
                    CLOId = clo.Id,
                    CLO = clo.Title,
                    QuestionCount = maTranList
                      .FirstOrDefault(m => m.ChapterId == ch.Id && m.CLOId == clo.Id)
                      ?.QuestionCount ?? 0,
                    MaxQuestionCount = deThi.ExamBank.QuestionBanks
                    .Count(c => c.ChapterId == ch.Id && c.CLOId == clo.Id)
                }).ToList()
            }).ToList();

            var result = new ExamMatrixVM
            {
                ExamId = id,
                ExamName = deThi.ExamName,
                Duration = deThi.Duration,
                Chapters = danhSachChuong
            };

            ViewBag.SubjectName = subjectName;
            ViewData["ChuongJson"] = JsonConvert.SerializeObject(chuongList);
            return View(result);
        }


        //Đề thi : Chỉnh sửa đề thi
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
            // With this corrected line:
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật đề thi thành công.";
            return RedirectToAction("Exam_Create_Matrix", new { id = MaDe });
        }


        //Đề thi : Lưu ma trận đề thi
        [HttpPost]
        public async Task<IActionResult> Save_Exam_Matrix([FromBody] ExamMatrixVM model)
        {
            // Kiểm tra xem model có hợp lệ không
            if (model == null || model.Chapters == null)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            var oldMatrix = await _context.ExamMatrices.Where(m => m.ExamId == model.ExamId).ToListAsync();

            // Xóa ma trận đề thi cũ
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


        //Đề thi : Cập nhật đề thi
        [HttpPost]
        public async Task<IActionResult> Update_Exam([FromBody] ExamMatrixVM model)
        {
            //Truy vấn đề thi theo ID
            var exam = await _context.Exams.FirstOrDefaultAsync(d => d.ExamId == model.ExamId);
            if (exam== null)
            {
                return Json(new { success = false, message = "Không tìm thấy đề thi." });
            }
            //Cập nhật thời gian làm bài
            exam.Duration = model.Duration;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        //Đề thi : Thêm đề thi vào lớp học
        [HttpPost]
        public async Task<IActionResult> Add_Exam_To_Class(ExamUnlockTimeVM vm, int maDe, int maLopHoc)
        {
            var deThi = await _context.Exams.FirstOrDefaultAsync(d => d.ExamId == maDe);
            var lopHoc = await _context.Classes.FirstOrDefaultAsync(l => l.ClassId == maLopHoc);

            if (deThi == null || lopHoc == null)
           {
                return NotFound();
            }

            var Them = new ClassExam
            {
                ClassId = maLopHoc,
                ExamId = maDe,
                IsExam = vm.LaDeThi, //Sua sau
                AddedDate = DateTime.Now,
                StartTime = vm.StartTime,
                EndTime = vm.EndTime,
                Access = false
            };

            _context.ClassExams.Add(Them);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = Them.IsExam ? "Thêm đề thi vào lớp học thành công!" : "Thêm đề ôn tập vào lớp học thành công!";

            var danhSachSVCuaLopHoc = await _context.ClassDetails
                .Include(sv => sv.User)
                .Include(lh => lh.Class)
                .Where(lh => lh.ClassId == maLopHoc).ToListAsync();

            if (Them.IsExam)
            {
                foreach (var sv in danhSachSVCuaLopHoc)
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
                                    <h2 style=""margin: 0; font-size: 26px;"">📢 THÔNG BÁO ĐỀ THI MỚI</h2>
                                </div>

                                <div class=""fade-in"" style=""padding: 32px;"">
                                    <p style=""font-size: 16px; margin-bottom: 16px;"">Xin chào <strong>" + sv.User.FullName + @"</strong>,</p>

                                    <p style=""font-size: 16px; line-height: 1.6;"">
                                        Một đề thi mới đã được giảng viên thêm vào lớp học <strong>" + sv.Class.ClassName + @"</strong> mà bạn đang tham gia.
                                    </p>

                                    <table style=""width: 100%; border-collapse: collapse; margin-top: 24px; font-size: 15px;"">
                                        <tr style=""background-color: #eef2f7;"">
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Tên đề thi</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + deThi.ExamName + @"</td>
                                        </tr>
                                        <tr>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Thời gian làm bài</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + deThi.Duration + @" phút</td>
                                        </tr>
                                        <tr style=""background-color: #eef2f7;"">
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Ngày bắt đầu</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + Them.StartTime.ToString("dd/MM/yyyy HH:mm:ss") + @"</td>
                                        </tr>
                                        <tr>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;""><strong>Ngày kết thúc</strong></td>
                                            <td style=""padding: 12px; border: 1px solid #d3dce6;"">" + Them.EndTime.ToString("dd/MM/yyyy HH:mm:ss") + @"</td>
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

                    BackgroundJob.Enqueue<EmailService>(service => service.SendAddExamNotification(sv.User.Email, sb.ToString(), true));

                }
            }

            return RedirectToAction("ClassDetail", new { id = maLopHoc });
        }


        //Đề thi : Lưu dữ liệu thêm đề thi
        [HttpPost]
        public async Task<IActionResult> Edit_Exam_Date(ExamUnlockTimeVM vm, int maDe, int maLopHoc)
        {
            if (ModelState.IsValid)
            {
                var classExam = await _context.ClassExams
                    .FirstOrDefaultAsync(x => x.ExamId == maDe);
                var lopHoc = await _context.ClassExams.FirstOrDefaultAsync(l => l.ClassId == maLopHoc);

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


        //Đề thi : Xóa đề thi trong một lớp học
        [HttpPost]
        public async Task<IActionResult> Delete_Exam(ExamUnlockTimeVM vm, int maDe, int maLopHoc)
        {
            if (ModelState.IsValid)
            {
                // Tìm bản ghi đề thi trong lớp
                var deThiLop = await _context.ClassExams
                    .FirstOrDefaultAsync(dt => dt.ExamId == maDe && dt.ClassId == maLopHoc);

                if (deThiLop != null)
                {
                    // Lấy tất cả lịch sử làm bài liên quan đến đề thi và lớp học này
                    var lichSuLamBais = await _context.ExamHistories
                        .Where(ls => ls.ExamId == maDe && ls.ClassId == maLopHoc)
                        .Include(ls => ls.StudentAnswers)
                        .ToListAsync();

                    // Xóa chi tiết bài làm trước
                    foreach (var lichSu in lichSuLamBais)
                    {
                        _context.StudentAnswers.RemoveRange(lichSu.StudentAnswers);
                    }

                    // Sau đó xóa lịch sử làm bài
                    _context.ExamHistories.RemoveRange(lichSuLamBais);

                    // Cuối cùng xóa đề thi khỏi lớp học
                    _context.ClassExams.Remove(deThiLop);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã xóa đề thi và toàn bộ lịch sử làm bài khỏi lớp học.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đề thi để xóa.";
                }
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
            var lichSuLamBai = await _context.ExamHistories
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

            if (lichSuLamBai == null)
            {
                return NotFound("Không tìm thấy lịch sử làm bài.");
            }

            return View(lichSuLamBai);
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

            _context.StudentAnswers.RemoveRange(lichSu.StudentAnswers);
            _context.ExamHistories.Remove(lichSu);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa lịch sử làm bài thành công!";
            return RedirectToAction("List_Student_Do_Test", new { id = maDe, maLopHoc = maLopHoc });
        }


        // Đề thi : update trạng thái xem lịch sử làm bài
        [HttpPost]
        public async Task<IActionResult> CapNhatAccess([FromBody] UpdateAccessVM model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "⚠️ Model null từ client" });
            }

            Console.WriteLine($"✅ Nhận dữ liệu: MaLopHoc={model.ClassId}, MaDe={model.ExamId}, Access={model.Access}");

            var deThiTrongLop = await _context.ClassExams
                .FirstOrDefaultAsync(x => x.ClassId == model.ClassId && x.ExamId == model.ExamId);

            if (deThiTrongLop == null)
            {
                return NotFound(new { success = false, message = "❌ Không tìm thấy đề thi trong lớp." });
            }

            deThiTrongLop.Access = model.Access;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "✅ Cập nhật quyền truy cập thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi lưu: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Lỗi server khi lưu dữ liệu!" });
            }
        }



        // Đề thi : Danh sách sinh viên của đề thi tự luận cần chấm bài
        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> Grading_The_Exam(int maDe, int maLop)
        {
            // 1. Lấy toàn bộ sinh viên trong lớp
            var allStudentsInClass = await _context.ClassDetails
                .Include(ct => ct.User)
                .Where(ct => ct.ClassId == maLop)
                .Select(ct => ct.User)
                .ToListAsync();

            // 2. Lấy danh sách đã nộp bài
            var examHistories = await _context.ExamHistories
                .Include(ls => ls.User)
                .Where(ls => ls.ExamId == maDe && ls.ClassId == maLop)
                .ToListAsync();

            // 3. Danh sách sinh viên đã làm bài
            var submittedStudents = examHistories.Select(sv => new StudentExamVM
            {
                UserId = sv.UserId,
                HistoryId = sv.ExamHistoryId,
                FullName = sv.User?.FullName,
                StudentID = sv.User?.Username,
                SubmissionTime = sv.SubmitTime,
                DurationInMinutes = sv.Duration,
                GradedScore = sv.Score
            }).ToList();

            // 4. Danh sách sinh viên chưa làm bài
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

            // 5. ViewModel
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
                    StudentAnswer = ct.EssayAnswer,
                    GradedScore = ct.Score
                }).ToList()
            };

            return View(vm);
        }



        //Đề thi : Lưu điểm chấm tự luận 
        [HttpPost]
        public async Task<IActionResult> SaveGradingResult(EssaySubmissionToGradeVM model)
        {
            var history = await _context.ExamHistories
                .Include(ls => ls.StudentAnswers)
                .FirstOrDefaultAsync(ls => ls.ExamHistoryId == model.HistoryId);

            if (history == null)
                return NotFound("Không tìm thấy bài làm");

            double totalScore = 0;
            int totalQuestions = model.ListQuestions.Count;

            // Tính tổng điểm đã chấm (cộng điểm từ từng câu)
            foreach (var question in model.ListQuestions)
            {
                var answer = history.StudentAnswers.FirstOrDefault(ct => ct.QuestionId == question.QuestionId);
                if (answer != null)
                {
                    // Gán điểm cho câu trả lời
                    answer.Score = question.GradedScore;

                    // Cộng vào tổng điểm đã chấm
                    totalScore += question.GradedScore ?? 0;
                }
            }

            // Tính điểm tối đa có thể đạt được
            double maxPossibleScore = totalQuestions * 10;

            // Tính tổng điểm chuẩn hóa
            double normalizedScore = (totalScore / maxPossibleScore) * 10;

            // Log tổng điểm chuẩn hóa
            Console.WriteLine($"Total Score: {totalScore}, Max Possible Score: {maxPossibleScore}, Normalized Score: {normalizedScore}");

            // Gán điểm chuẩn hóa vào lịch sử bài thi
            history.Score = Math.Round(normalizedScore, 2);
            history.SubmitTime = history.SubmitTime ?? DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Chấm điểm thành công!";
            return RedirectToAction("Grading_The_Exam", new { maDe = history.ExamId, maLop = history.ClassId });
        }



        #endregion

        //================================================================

        #region NganHangDe
        //Ngân hàng đề : Danh sách ngân hàng đề
        public async Task<IActionResult> ExamBank()
        {
            // Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // Truy vấn danh sách ngân hàng đề của giảng viên
            var danhSachNHDRiengTu = await _context.ExamBanks
                .Where(nh => nh.UserId == user.UserId)
                .ToListAsync();

            var danhSachNHDCongKhai = await _context.ExamBanks
                .Where(nh => nh.Status == true)
                .ToListAsync();

            var danhSachRT = danhSachNHDRiengTu.Select(nh => new PrivateExamBankVM
            {
                ExamBankId = nh.ExamBankId,
                ExamBankName = nh.ExamBankName,
                CreatedDate = nh.CreatedDate,
                IsActive = nh.Status,
                ExamType = nh.ExamType
            }).ToList();

            var danhSachCK = danhSachNHDCongKhai.Select(nh => new PublicExamBankVM
            {
                ExamBankId = nh.ExamBankId,
                CreatedDate = nh.CreatedDate,
                ExamBankName = nh.ExamBankName,
                ExamType = nh.ExamType,
                LecturerId = nh.UserId,
                LecturerName = _context.Users
                    .SingleOrDefault(gvien => gvien.UserId == nh.UserId)?.FullName
            }).ToList();

            var result = new ExamBankListVM
            {
                PrivateExamBanks = danhSachRT,
                PublicExamBanks = danhSachCK
            };

            return View(result);
        }



        //Ngân hàng đề : Chi tiết ngân hàng đề
        public async Task<IActionResult> Exam_Bank_Detail(int id)
        {
            // Truy vấn ngân hàng đề
            var examBank = await _context.ExamBanks.FindAsync(id);
            if (examBank == null)
                return NotFound("Không tìm thấy ngân hàng đề.");

            int subjectId = examBank.SubjectId;

            // Gọi API lấy danh sách môn học (gồm chương và CLO)
            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Không thể lấy dữ liệu môn học.");

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root["data"];
            var subjectList = dataToken.ToObject<List<SubjectDto>>();
            var monHoc = subjectList.FirstOrDefault(s => s.Id == subjectId);
            if (monHoc == null) return NotFound();
            var danhSachChuong = monHoc?.Chapters ?? new List<ChapterDto>();

            var thongTinNHD = await _context.ExamBanks
                .Where(ch => ch.ExamBankId == id)
                .Select(ch => new
                {
                    ch.ExamBankName,
                    ch.CreatedDate,
                    ch.ExamType
                })
                .FirstOrDefaultAsync();

            // Lấy danh sách câu hỏi từ ngân hàng đề
            var examQuestions = await _context.QuestionBanks
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
                ExamBankName = thongTinNHD?.ExamBankName,
                CreatedDate = thongTinNHD?.CreatedDate,
                ExamType = thongTinNHD?.ExamType,
                Subject = monHoc,
                ExamBankQuestions = examQuestions
            };

            return View(viewModel);
        }


        //Ngân hàng đề : Tạo một ngân hàng đề thi
        [HttpGet]
        public async Task<IActionResult> Exam_Bank_Create()
        {
            string access_token = Request.Cookies["access_token"];

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl); //fixed

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Không thể lấy danh sách môn học.");

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root["data"];

            var subjectList = dataToken.ToObject<List<SubjectDto>>();

            // Đưa danh sách môn học vào ViewBag để hiển thị trong dropdown
            ViewBag.Subjects = subjectList.Select(s => new CreateExamBankVM
            {
                SubjectId = s.Id,
                SubjectCode = s.Code,
                SubjectName = s.Name
            }).ToList();

            return View();
        }


        //Ngân hàng đề : Chỉnh sửa ngân hàng đề
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

            // Kiểm tra loại đề
            if (examBank.ExamType == "Tự luận")
            {
                return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = MaNHD });
            }
            else // mặc định hoặc loại khác là trắc nghiệm
            {
                return RedirectToAction("Exam_Bank_Add_Question_Multiplechoice", new { id = MaNHD });
            }
        }


        //Ngân hàng đề : Lưu một ngân hàng đề đã tạo
        [HttpPost]
        public async Task<IActionResult> Exam_Bank_Create(CreateExamBankVM model)
        {
            // Kiểm tra tài khoản giảng viên
            var access_token = Request.Cookies["access_token"];
            var user = await _context.Users.FirstOrDefaultAsync(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            // Tạo đối tượng ExamBank từ ViewModel
            var newExamBank = new ExamBank
            {
                ExamBankName = model.ExamBankName,
                CreatedDate = DateOnly.FromDateTime(DateTime.Now).ToDateTime(TimeOnly.MinValue),
                Status = model.Status,
                SubjectId = model.SubjectId,
                ExamType = model.ExamType,
                UserId = user.UserId
            };

            _context.Add(newExamBank);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tạo ngân hàng đề thành công!";

            if (newExamBank.ExamType == "Trắc nghiệm")
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = newExamBank.ExamBankId });
            else
                return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = newExamBank.ExamBankId });
        }


        //Ngân hàng đề : Câu hỏi : Thêm câu hỏi trắc nghiệm
        public async Task<IActionResult> Exam_Bank_Add_Question_MultipleChoice(int id)
        {
            // Truy vấn ngân hàng đề bằng id
            var examBank = await _context.ExamBanks.FindAsync(id);
            if (examBank == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            int subjectId = examBank.SubjectId;

            // Gọi API lấy danh sách môn học (gồm chương & CLO)
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
            var dataToken = root["data"];
            var subjectList = dataToken.ToObject<List<SubjectDto>>();

            // Lấy môn học tương ứng
            var monHoc = subjectList.FirstOrDefault(s => s.Id == subjectId);
            if (monHoc == null)
                return NotFound("Không tìm thấy môn học.");

            var danhSachChuong = monHoc.Chapters ?? new List<ChapterDto>();

            // Lấy danh sách câu hỏi trong ngân hàng đề
            var questionList = await _context.QuestionBanks
                .Where(q => q.ExamBankId == id)
                .ToListAsync();

            // Map sang DTO
            var danhSachCauHoiDTO = questionList.Select(ch => new QuestionDto
            {
                QuestionId = ch.QuestionId,
                QuestionContent = ch.QuestionContent,
                ChoiceContent = ch.ChoiceContent,
                QuestionType = ch.QuestionType,
                CloId = ch.CLOId ?? 0,
                ChapterId = ch.ChapterId ?? 0
            }).ToList();

            var viewModel = new CreateQuestionVM
            {
                ExamBankId = id,
                ExamBankName = examBank.ExamBankName,
                IsActive = examBank.Status,
                QuestionList = questionList,
                QuestionDTOList = danhSachCauHoiDTO,
                ChapterList = monHoc?.Chapters ?? new List<ChapterDto>()
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
            var subjectList = root["data"].ToObject<List<SubjectDto>>();
            var subject = subjectList.FirstOrDefault(s => s.Id == subjectId);
            model.ChapterList = subject?.Chapters ?? new List<ChapterDto>();

            var luaChonList = new List<object>();
            for (int i = 0; i < model.ChoiceContents.Count; i++)
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



        //Ngân hàng đề : Câu hỏi : Thêm câu hỏi tự luận
        public async Task<IActionResult> Exam_Bank_Add_Question_Essay(int id)
        {
            var examBank = await _context.ExamBanks.FindAsync(id);
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
            var dataToken = root["data"];
            var subjectList = dataToken.ToObject<List<SubjectDto>>();

            var subject = subjectList.FirstOrDefault(s => s.Id == subjectId);
            if (subject == null)
                return NotFound("Không tìm thấy môn học.");

            var danhSachChuong = subject.Chapters ?? new List<ChapterDto>();

            var danhSachCauHoi = await _context.QuestionBanks
                .Where(ch => ch.ExamBankId == id)
                .ToListAsync();

            var danhSachCauHoiDTO = danhSachCauHoi.Select(ch => new QuestionDto
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
                QuestionList = danhSachCauHoi,
                QuestionDTOList = danhSachCauHoiDTO,
                ChapterList = danhSachChuong
            };

            return View(viewModel);
        }



        //Ngân hàng đề : Câu hỏi : Lưu câu hỏi tự luận
        [HttpPost]
        public async Task<IActionResult> Exam_Bank_Add_Question_Essay(CreateQuestionVM model)
        {
            string errMessage = "";

            // Kiểm tra nội dung câu hỏi
            if (string.IsNullOrWhiteSpace(model.QuestionContent))
            {
                errMessage = "Nội dung câu hỏi không được bỏ trống.";
            }

            // Nếu có lỗi, trả về view cùng với thông báo lỗi
            if (!string.IsNullOrEmpty(errMessage))
            {
                TempData["ErrorMessage"] = errMessage;
                return RedirectToAction("Exam_Bank_Add_Question_Essay", new { id = model.ExamBankId });
            }

            // Truy vấn ngân hàng đề
            var examBank = await _context.ExamBanks.FindAsync(model.ExamBankId);
            if (examBank == null)
            {
                return NotFound("Không tìm thấy ngân hàng đề.");
            }

            int subjectId = examBank.SubjectId;

            // Lấy danh sách chương và CLO
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
            var subjectList = root["data"].ToObject<List<SubjectDto>>();
            var subject = subjectList.FirstOrDefault(s => s.Id == subjectId);

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

            // Kiểm tra các bảng liên quan
            var isRelatedToExams = await _context.Exams.AnyAsync(e => e.ExamBankId == MaNHD && e.ExamName.Contains(cauHoi.QuestionId.ToString()));
            var isRelatedToAnswers = await _context.StudentAnswers.AnyAsync(sa => sa.QuestionId == maCauHoi);

            // Tạo thông báo liên quan
            string relatedInfo = "";
            if (isRelatedToExams)
            {
                relatedInfo += "Câu hỏi này có liên quan đến một số bài thi.\n";
            }
            if (isRelatedToAnswers)
            {
                relatedInfo += "Câu hỏi này có liên quan đến một số câu trả lời của học sinh.\n";
            }

            if (!string.IsNullOrEmpty(relatedInfo))
            {
                TempData["ErrorMessage"] = $"Không thể xóa câu hỏi vì nó đang được sử dụng trong các bảng khác:\n{relatedInfo}";
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", new { id = MaNHD });
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




        //Lớp học: Xóa ngân lớp học
        [HttpPost]
        public async Task<IActionResult> DeleteClass(int maLopHoc)
        {
            var lopHoc = await _context.Classes
                .Include(l => l.ClassDetails)
                .Include(l => l.ClassExams)
                .Include(l => l.ExamHistories)
                    .ThenInclude(ls => ls.StudentAnswers)
                .FirstOrDefaultAsync(l => l.ClassId == maLopHoc);

            if (lopHoc == null)
                return NotFound();

            // Xóa StudentAnswers trong từng ExamHistory
            foreach (var lichSu in lopHoc.ExamHistories)
            {
                if (lichSu.StudentAnswers != null)
                {
                    _context.StudentAnswers.RemoveRange(lichSu.StudentAnswers);
                }
            }

            // Xóa ExamHistories
            _context.ExamHistories.RemoveRange(lopHoc.ExamHistories);

            // Xóa ClassDetails
            _context.ClassDetails.RemoveRange(lopHoc.ClassDetails);

            // Xóa ClassExams
            _context.ClassExams.RemoveRange(lopHoc.ClassExams);

            // Cuối cùng xóa Classes
            _context.Classes.Remove(lopHoc);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa lớp học và các dữ liệu liên quan thành công!";
            return RedirectToAction("Class");
        }



        //Ngân hàng đề : Xóa ngân hàng đề 
        [HttpPost]
        public async Task<IActionResult> XoaNganHangDe(int maNHD)
        {
            var nganHangDe = await _context.ExamBanks
                .Include(nhd => nhd.Exams)
                .Include(nhd => nhd.QuestionBanks)
                .FirstOrDefaultAsync(nhd => nhd.ExamBankId == maNHD);

            Console.WriteLine($"maNHD nhận được: {maNHD}");
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


            // Cuối cùng xóa ngân hàng đề
            _context.ExamBanks.Remove(nganHangDe);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa ngân hàng đề thành công!";
            return RedirectToAction("ExamBank");
        }

        //Ngân hàng đề : lấy đề thi theo ngân hàng đề
        [HttpGet]
        public async Task<IActionResult> GetExamsByExamBank(int maNHD)
        {
            var exams = await _context.Exams
                .Where(e => e.ExamBankId == maNHD)
                .Select(e => new {
                    e.ExamId,
                    e.ExamName,
                    e.CreatedDate
                })
                .ToListAsync();

            return Ok(exams);
        }
        //Ngân hàng đề : xóa đề thi theo ngân hàng đề
        [HttpPost]
        public async Task<IActionResult> DeleteExamByExamBank(int examId)
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
            return RedirectToAction("ExamBank");
        }




        #endregion

        //================================================================

        #region Monitoring
        public IActionResult Monitoring(int examId, int classId)
        {
            var access_token = Request.Cookies["access_token"];
            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                return RedirectToAction("Logout", "Auth");
            }

            if (user.Role?.ToLower() != "teacher")
            {
                return NotFound();
            }

            var Class = _context.Classes
                .Include(l => l.ClassDetails)
                .ThenInclude(cd => cd.User)
                .FirstOrDefault(l => l.ClassId == classId);

            if (Class == null)
            {
                return NotFound();
            }

            var Exam = _context.Exams.SingleOrDefault(dt => dt.ExamId == examId);
            if (Exam == null)
            {
                return NotFound();
            }

            ViewBag.Instructor = user.FullName;

            var StudentList = Class.ClassDetails
                .Where(cd => cd.User != null)
                .Select(cd => new StudentVM
                {
                    UserId = cd.UserId,
                    FullName = cd.User.FullName,
                    Username = cd.User.Username
                }).ToList();

            var viewModel = new MonitoringVM
            {
                ExamId = Exam.ExamId,
                ExamName = Exam.ExamName,
                ExamType = Exam.ExamType,
                Duration = Exam.Duration,
                ClassId = Class.ClassId,
                ClassName = Class.ClassName,
                StudentList = StudentList
            };

            return View(viewModel);
        }

        #endregion

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
