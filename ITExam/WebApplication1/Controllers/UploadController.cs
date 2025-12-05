using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net; 

namespace ITExam.Controllers
{
    [Route("Upload")]
    public class UploadController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<UploadController> _logger;
        private const long MaxFileBytes = 10 * 1024 * 1024;

        public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
        {
            _env = env;
            _logger = logger;
        }

        [HttpPost("UploadImage")]
        [RequestSizeLimit(10_000_000)] 
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload == null || upload.Length == 0)
                return BadRequest(new { error = new { message = "Không có file nào được gửi lên." } });

            if (upload.Length > MaxFileBytes)
            {
                return BadRequest(new { error = new { message = "File vượt quá giới hạn 10MB." } });
            }

            var allowed = new[]
            {
                "image/jpeg", "image/jpg", "image/pjpeg",
                "image/png", "image/webp", "image/gif",
                "image/bmp", "image/heic", "image/heif"
            };

            var contentType = upload.ContentType.ToLowerInvariant();
            if (!allowed.Contains(contentType))
            {
                return BadRequest(new { error = new { message = $"Định dạng {contentType} không được hỗ trợ." } });
            }

            try
            {
                var ext = Path.GetExtension(upload.FileName);
                if (string.IsNullOrWhiteSpace(ext))
                {
                    ext = contentType switch
                    {
                        "image/png" => ".png",
                        "image/webp" => ".webp",
                        "image/gif" => ".gif",
                        "image/bmp" => ".bmp",
                        "image/heic" => ".heic",
                        "image/heif" => ".heif",
                        _ => ".jpg"
                    };
                }

                var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(webRootPath, "uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var newFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
                var filePath = Path.Combine(uploadsFolder, newFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var imageUrl = $"{baseUrl}/uploads/{newFileName}";

                _logger.LogInformation($"Đã upload ảnh: {filePath}");

                return Json(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi upload ảnh CKEditor");
                return StatusCode(500, new { error = new { message = "Lỗi server khi lưu ảnh." } });
            }
        }
    }
}