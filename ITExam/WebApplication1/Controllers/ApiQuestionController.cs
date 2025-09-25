using ITExam.ExternalModels.Subject;
using ITExam.Services;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ITExam.Models;
using Microsoft.EntityFrameworkCore;
using ITExam.ViewModels.ExamBank;
using ITExam.ExternalModels;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Distributed;
using ITExam.Filters;

namespace ITExam.Controllers
{
    [CheckToken]
    [AuthorizeRole("teacher")]
    public class ApiQuestionController : Controller
    {
        private readonly ITExamDbContext _context;
        private readonly GeminiService _geminiService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRedisService _redis;

        public ApiQuestionController(IHttpClientFactory httpClientFactory, ITExamDbContext context, GeminiService geminiService, IRedisService redis)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _geminiService = geminiService;
            _redis = redis;
        }

        public async Task<IActionResult> Index(int id)
        {
            var examBank = await _context.ExamBanks.FindAsync(id); //Truy van ngan hang de
            if (examBank == null) return NotFound("Không tìm thấy ngân hàng đề.");
            var subject = await GetSubjectByIdAsync(examBank.SubjectId);
            if (subject == null) return NotFound("Không tìm thấy dữ liệu môn học từ API.");

            var chapters = subject.Chapters.Select((chuong, index) => new ChapterCLOVM //Lay danh sach chuong va CLO
            {
                ChapterId = chuong.Id,
                ChapterName = $"Chương {index + 1}: {chuong.Title}",
                Clos = chuong.Clos.Select(clo => new CLOItem
                {
                    CLOId = clo.Id,
                    CLOTitle = clo.Title,
                    CLODescription = clo.Description
                }).ToList()
            }).ToList();

            var viewModel = new CreateQuestionWithGeminiVM
            {
                ExamBankId = examBank.ExamBankId,
                ExamBankName = examBank.ExamBankName,
                SubjectName = subject.Name,
                Chapters = chapters
            };
            return View(viewModel);
        }
        private async Task<SubjectDto> GetSubjectByIdAsync(int masubject)
        {
            var access_token = Request.Cookies["access_token"];
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            var response = await client.GetAsync(RoutingAPI.GetSubjectUrl);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JObject>(json);
            var dataToken = root["data"];
            var subjectList = dataToken.ToObject<List<SubjectDto>>();
            return subjectList.FirstOrDefault(s => s.Id == masubject);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestionFromGemini(string? requests, int id, GenerateQuestionsInputModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu ma trận không hợp lệ.";
                return RedirectToAction(nameof(Index), new { id = id });
            }

            var examBank = await _context.ExamBanks.FindAsync(id);
            if (examBank == null) return NotFound("Không tìm thấy ngân hàng đề.");

            var subject = await GetSubjectByIdAsync(examBank.SubjectId);
            if (subject == null) return NotFound("Không tìm thấy môn học");

            var allGeneratedQuestions = new List<DatabaseQuestion>();
            foreach (var matrixRow in model.MatrixRows.Where(r => r.CloInputs.Any(c => c.QuestionCount > 0)))
            {
                var chapter = subject.Chapters.FirstOrDefault(c => c.Id == matrixRow.ChapterId);
                if (chapter == null) continue;
                foreach (var cloInput in matrixRow.CloInputs.Where(c => c.QuestionCount > 0))
                {
                    var clo = chapter.Clos.FirstOrDefault(c => c.Id == cloInput.CloId);
                    if (clo == null) continue;
                    string prompt = $"Tạo cho tôi {cloInput.QuestionCount} câu hỏi trắc nghiệm (gồm 4 lựa chọn, chỉ có 1 đáp án đúng) về chuyên đề sau:\n" +
                                    $"- Môn học: {subject.Name}\n" +
                                    $"- Nội dung chính thuộc Chương: {chapter.Title}\n" +
                                    $"- Câu hỏi phải đánh giá được Chuẩn đầu ra (CLO): [{clo.Title}] {clo.Description}\n" +
                                    $"Yêu cầu câu hỏi phải phù hợp và trực tiếp liên quan đến CLO đã nêu." +
                                    $"Yêu cầu thêm là: " + requests;
                    try
                    {
                        var questionsFromAI = await _geminiService.GetQuestionsFromGeminiAsync(prompt);
                        foreach (var q in questionsFromAI)
                        {
                            q.ChapterId = chapter.Id;
                            q.CloId = clo.Id;
                        }
                        allGeneratedQuestions.AddRange(questionsFromAI);
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = $"Lỗi khi tạo câu hỏi cho Chương {chapter.Title}: {ex.Message}";
                        return RedirectToAction(nameof(Index), new { id = id });
                    }
                }
            }

            var reviewViewModel = new ReviewQuestionsVM
            {
                ExamBankId = id,
                SubjectData = subject,
                GeneratedQuestions = allGeneratedQuestions
            };

            var reviewSessionKey = $"review_questions_{Guid.NewGuid()}";
            await _redis.SetStringAsync(reviewSessionKey, JsonConvert.SerializeObject(reviewViewModel));

            return RedirectToAction(nameof(ReviewGeneratedQuestions), new { reviewId = reviewSessionKey });
        }

        [HttpGet]
        public async Task<IActionResult> ReviewGeneratedQuestions(string reviewId)
        {
            if (string.IsNullOrEmpty(reviewId))
            {
                TempData["ErrorMessage"] = "Yêu cầu không hợp lệ.";
                return RedirectToAction("Index", "Teacher");
            }

            var viewModelJson = await _redis.GetStringAsync(reviewId);
            await _redis.DeleteKeyAsync(reviewId);

            if (string.IsNullOrEmpty(viewModelJson))
            {
                TempData["ErrorMessage"] = "Phiên xem lại đã hết hạn hoặc không tồn tại. Vui lòng tạo lại.";
                return RedirectToAction("Index", "Teacher");
            }

            var viewModel = JsonConvert.DeserializeObject<ReviewQuestionsVM>(viewModelJson);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGeneratedQuestions(int examBankId, List<DatabaseQuestion> questions)
        {
            if (ModelState.IsValid)
            {
                var newQuestionsForDb = new List<QuestionBank>();

                foreach (var q in questions)
                {
                    foreach (var choice in q.Choices)
                    {
                        choice.LaDapAn = (choice.Id == q.CorrectChoiceId);
                    }


                    var dbQuestion = new QuestionBank
                    {
                        ExamBankId = examBankId,
                        QuestionContent = q.QuestionContent,
                        ChoiceContent = JsonConvert.SerializeObject(q.Choices),
                        ChapterId = q.ChapterId,
                        CLOId = q.CloId,
                        QuestionType = "1"
                    };
                    newQuestionsForDb.Add(dbQuestion);
                }

                await _context.QuestionBanks.AddRangeAsync(newQuestionsForDb);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã thêm thành công {newQuestionsForDb.Count} câu hỏi vào ngân hàng đề.";
                return RedirectToAction("Exam_Bank_Add_Question_MultipleChoice", "Teacher", new { id = examBankId });
            }
            ViewBag.examBanksId = examBankId;
            return View("ReviewGeneratedQuestions", questions);
        }
    }
}
