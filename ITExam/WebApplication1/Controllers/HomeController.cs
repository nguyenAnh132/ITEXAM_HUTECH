using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities; // <-- để dùng QueryHelpers
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // ===================== GOOGLE OAUTH START =====================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoToGoogleAuth([FromServices] IWebHostEnvironment env)
        {
            try
            {
                string credentialsPath = Path.Combine(env.ContentRootPath, "key_secret.json");
                if (!System.IO.File.Exists(credentialsPath))
                {
                    return Content("Không tìm thấy key_secret.json tại: " + credentialsPath);
                }

                GoogleClientSecrets clientSecrets;
                using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    clientSecrets = GoogleClientSecrets.FromStream(stream);
                }

                string redirectUri = "https://localhost:7231/signin-google";

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets.Secrets,
                    Scopes = new[] { DriveService.Scope.Drive }
                });

                // Tạo request cơ bản
                var baseReq = flow.CreateAuthorizationCodeRequest(redirectUri);

                // Thử ép kiểu về GoogleAuthorizationCodeRequestUrl để gán trực tiếp
                var googleReq = baseReq as GoogleAuthorizationCodeRequestUrl;
                string authUrl;

                if (googleReq != null)
                {
                    // ĐƯỜNG CHÍNH: gán thuộc tính trên lớp Google
                    googleReq.AccessType = "offline";

                    // Nếu bản thư viện có 'Prompt' thì dùng:
                    try
                    {
                        googleReq.Prompt = "consent"; // buộc hiện consent -> lấy refresh_token
                    }
                    catch
                    {
                        // Nếu không có 'Prompt' (bản rất cũ), fallback 'ApprovalPrompt'
                        googleReq.ApprovalPrompt = "force";
                    }

                    authUrl = googleReq.Build().AbsoluteUri;
                }
                else
                {
                    // Fallback: chèn query param bằng QueryHelpers (không cần ExtraParameters)
                    var url = baseReq.Build().AbsoluteUri;
                    url = QueryHelpers.AddQueryString(url, "access_type", "offline");
                    // thử dùng prompt trước
                    url = QueryHelpers.AddQueryString(url, "prompt", "consent");
                    // nếu thư viện cũ/Google bỏ qua, cũng không sao; vẫn redirect bình thường
                    authUrl = url;
                }

                _logger.LogInformation(">>> Redirect đến Google OAuth: {Url}", authUrl);
                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI KHI TẠO AUTH URL");
                return Content($"Lỗi khi tạo Auth URL: {ex.Message}. Đã đặt file key_secret.json đúng chỗ chưa?");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("/signin-google")]
        public async Task<IActionResult> signin_google(string code, [FromServices] IWebHostEnvironment env)
        {
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogError(">>> Google redirect không có 'code'.");
                return Content("Lỗi: Google redirect không có 'code'.");
            }

            try
            {
                string credentialsPath = Path.Combine(env.ContentRootPath, "key_secret.json");
                var saveFolder = Path.Combine(env.ContentRootPath, "App_Data", "oauth");
                Directory.CreateDirectory(saveFolder);               // đảm bảo tồn tại
                string tokenPath = Path.Combine(saveFolder, "token.json");
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var redirectUri = $"{baseUrl}/signin-google";

                if (!System.IO.File.Exists(credentialsPath))
                {
                    return Content("Không tìm thấy key_secret.json tại: " + credentialsPath);
                }

                GoogleClientSecrets clientSecrets;
                using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    clientSecrets = GoogleClientSecrets.FromStream(stream);
                }

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets.Secrets,
                    Scopes = new[] { DriveService.Scope.Drive }
                });

                TokenResponse token = await flow.ExchangeCodeForTokenAsync(
                    userId: "user",
                    code: code,
                    redirectUri: redirectUri,
                    taskCancellationToken: CancellationToken.None
                );

                if (string.IsNullOrEmpty(token.RefreshToken))
                {
                    _logger.LogError(">>> LỖI: KHÔNG NHẬN ĐƯỢC REFRESH TOKEN.");
                    return Content("LỖI: KHÔNG NHẬN ĐƯỢC REFRESH TOKEN. Hãy đảm bảo AccessType='offline' & Prompt='consent' (hoặc ApprovalPrompt='force' với bản cũ). Nếu đã từng cấp quyền, hãy vào Google Account > Security > Third-party access, remove app rồi authorize lại.");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var tokenJson = JsonSerializer.Serialize(token, options);
                await System.IO.File.WriteAllTextAsync(tokenPath, tokenJson);

                _logger.LogInformation(">>> ĐÃ LƯU token.json THÀNH CÔNG.");
                return Content("Đã lưu token.json thành công. Bạn có thể restart server và dùng refresh_token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LỖI KHI ĐỔI CODE LẤY TOKEN");
                return Content($"Lỗi khi đổi code lấy token: {ex.Message}");
            }
        }

        // ===================== GOOGLE OAUTH END =====================
    }
}
