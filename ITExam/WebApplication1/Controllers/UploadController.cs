using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ITExam.Controllers
{
    [Route("Upload")]
    public class UploadController : Controller
    {
        private const long MaxFileBytes = 2 * 1024 * 1024; // 2MB
        private static readonly string[] AllowedContentTypes = new[]
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        private readonly IHttpClientFactory _httpClientFactory;

        public UploadController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Giới hạn form-body 2MB để chặn từ sớm (khớp với MaxFileBytes).
        /// </summary>
        [HttpPost("UploadImage")] 
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileBytes)]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload == null || upload.Length == 0)
                return BadRequest(new { error = new { message = "File vượt quá giới hạn 2MB. Vui lòng chọn ảnh <= 2MB." } });

            // Kiểm tra kích thước 2MB
            if (upload.Length > MaxFileBytes)
            {
                // 413: Payload Too Large
                return StatusCode(StatusCodes.Status413PayloadTooLarge,
                    new { error = new { message = "File vượt quá giới hạn 2MB. Vui lòng chọn ảnh <= 2MB." } });
            }

            // Kiểm tra loại MIME cơ bản (tùy chọn nhưng nên có)
            if (!AllowedContentTypes.Contains(upload.ContentType))
            {
                return BadRequest(new { error = new { message = "Chỉ chấp nhận các định dạng: JPG, PNG, GIF, WEBP." } });
            }

            using var ms = new MemoryStream();
            await upload.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            // Base64 data URI (giữ nguyên cách bạn đang dùng)
            string base64File = Convert.ToBase64String(fileBytes);
            string mimeType = upload.ContentType; // ví dụ "image/png"
            string base64WithMime = $"data:{mimeType};base64,{base64File}";

            // CKEditor 5 SimpleUploadAdapter chỉ cần { url: ... }
            return Json(new { url = base64WithMime });
        }
    }
}
