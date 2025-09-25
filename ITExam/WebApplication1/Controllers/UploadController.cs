using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace ITExam.Controllers
{
    [Route("Upload")]
    public class UploadController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UploadController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload == null || upload.Length == 0)
                return BadRequest("Không có file được gửi lên");

            using var ms = new MemoryStream();
            await upload.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            // Chuyển sang base64
            string base64File = Convert.ToBase64String(fileBytes);

            // Nếu muốn kèm kiểu MIME để hiển thị ảnh trực tiếp trong HTML
            string mimeType = upload.ContentType; // ví dụ: "image/png"
            string base64WithMime = $"data:{mimeType};base64,{base64File}";

            return Json(new
            {
                uploaded = true,
                fileName = upload.FileName,
                url = base64WithMime 
            });

        }

    }
}
