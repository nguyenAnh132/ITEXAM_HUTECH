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
    public class AdminSubjectsController : Controller
    {
        private readonly ITExamDbContext _context;
        private readonly ISubjectAdminService _subjectAdminService;

        public AdminSubjectsController(ITExamDbContext context, ISubjectAdminService subjectAdminService)
        {
            _context = context;
            _subjectAdminService = subjectAdminService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var subjects = await _subjectAdminService.GetSubjectsAsync();
            return View(subjects);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SubjectCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubjectCreateVM model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _subjectAdminService.CreateSubjectAsync(model);
                TempData["SuccessMessage"] = "Tạo môn học thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int subjectId)
        {
            var subject = await _context.Subjects.AsNoTracking().SingleOrDefaultAsync(s => s.Id == subjectId);
            if (subject == null) return NotFound();

            var vm = new SubjectEditVM
            {
                Id = subject.Id,
                Code = subject.Code,
                Name = subject.Name
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubjectEditVM model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _subjectAdminService.UpdateSubjectAsync(model);
                TempData["SuccessMessage"] = "Cập nhật môn học thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int subjectId)
        {
            try
            {
                await _subjectAdminService.DeleteSubjectAsync(subjectId);
                TempData["SuccessMessage"] = "Xóa môn học thành công!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Chapters(int subjectId)
        {
            var subject = await _context.Subjects.AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == subjectId);
            if (subject == null) return NotFound();

            var chapters = await _subjectAdminService.GetChaptersWithClosAsync(subjectId);
            ViewData["SubjectId"] = subjectId;
            ViewData["SubjectName"] = subject.Name;
            return View(chapters);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChapter(ChapterCreateVM model)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(Chapters), new { subjectId = model.SubjectId });

            try
            {
                await _subjectAdminService.CreateChapterAsync(model);
                TempData["SuccessMessage"] = "Thêm chương thành công!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Chapters), new { subjectId = model.SubjectId });
        }

        [HttpGet]
        public async Task<IActionResult> EditChapter(int chapterId)
        {
            var chapter = await _context.SubjectChapters.AsNoTracking().SingleOrDefaultAsync(ch => ch.Id == chapterId);
            if (chapter == null) return NotFound();

            var vm = new ChapterEditVM
            {
                ChapterId = chapter.Id,
                Title = chapter.Title
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditChapter(ChapterEditVM model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _subjectAdminService.UpdateChapterAsync(model);
                TempData["SuccessMessage"] = "Cập nhật chương thành công!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            var chapter = await _context.SubjectChapters.AsNoTracking().SingleOrDefaultAsync(ch => ch.Id == model.ChapterId);
            if (chapter == null) return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Chapters), new { subjectId = chapter.SubjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteChapter(int chapterId)
        {
            var chapter = await _context.SubjectChapters.AsNoTracking().SingleOrDefaultAsync(ch => ch.Id == chapterId);
            if (chapter == null) return NotFound();

            try
            {
                await _subjectAdminService.DeleteChapterAsync(chapterId);
                TempData["SuccessMessage"] = "Xóa chương thành công!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Chapters), new { subjectId = chapter.SubjectId });
        }

        // ---------------- Clos ----------------
        [HttpGet]
        public async Task<IActionResult> Clos(int chapterId)
        {
            var chapter = await _context.SubjectChapters.AsNoTracking()
                .SingleOrDefaultAsync(ch => ch.Id == chapterId);
            if (chapter == null) return NotFound();

            var clos = await _subjectAdminService.GetClosByChapterAsync(chapterId);
            var subject = await _context.Subjects.AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == chapter.SubjectId);
            ViewData["SubjectId"] = chapter.SubjectId;
            ViewData["SubjectName"] = subject?.Name ?? "";
            ViewData["ChapterId"] = chapterId;
            ViewData["ChapterTitle"] = chapter.Title;
            return View(clos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClo(CloCreateVM model)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(Clos), new { chapterId = model.ChapterId });

            try
            {
                await _subjectAdminService.CreateCloAsync(model);
                TempData["SuccessMessage"] = "Thêm CLO thành công!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Clos), new { chapterId = model.ChapterId });
        }

        [HttpGet]
        public async Task<IActionResult> EditClo(int cloId)
        {
            var clo = await _context.SubjectClos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cloId);
            if (clo == null) return NotFound();

            var vm = new CloEditVM
            {
                CloId = clo.Id,
                Title = clo.Title,
                Description = clo.Description
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClo(CloEditVM model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _subjectAdminService.UpdateCloAsync(model);
                TempData["SuccessMessage"] = "Cập nhật CLO thành công!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            var clo = await _context.SubjectClos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == model.CloId);
            if (clo == null) return RedirectToAction(nameof(Index));
            return RedirectToAction(nameof(Clos), new { chapterId = clo.ChapterId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClo(int cloId)
        {
            var clo = await _context.SubjectClos.AsNoTracking().SingleOrDefaultAsync(c => c.Id == cloId);
            if (clo == null) return NotFound();

            try
            {
                await _subjectAdminService.DeleteCloAsync(cloId);
                TempData["SuccessMessage"] = "Xóa CLO thành công!";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Clos), new { chapterId = clo.ChapterId });
        }
    }
}

