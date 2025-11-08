using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class GoogleDriveService
{
    // Thư mục đích trong My Drive của BẠN
    private readonly string _parentFolderId = "1nHSqnnrJftgkG7tQUoYsxzqDR2AmxC_8"; // TODO: đổi ID của bạn

    private readonly DriveService _service;
    private readonly ILogger<GoogleDriveService> _logger;

    // Paths
    private readonly string _credentialsPath; // key_secret.json
    private readonly string _tokenFolder;     // App_Data/oauth
    private readonly string _tokenPath;       // App_Data/oauth/token.json

    public GoogleDriveService(IWebHostEnvironment env, ILogger<GoogleDriveService> logger)
    {
        _logger = logger;

        // 1) key_secret.json vẫn để ở gốc project (chỉ đọc)
        _credentialsPath = Path.Combine(env.ContentRootPath, "key_secret.json");

        // 2) token.json chuyển sang thư mục có quyền ghi
        _tokenFolder = Path.Combine(env.ContentRootPath, "App_Data", "oauth");
        Directory.CreateDirectory(_tokenFolder); // đảm bảo tồn tại
        _tokenPath = Path.Combine(_tokenFolder, "token.json");

        try
        {
            if (!File.Exists(_credentialsPath))
                throw new FileNotFoundException($"Không tìm thấy key_secret.json tại: {_credentialsPath}");

            _service = GetDriveService(_credentialsPath, _tokenPath).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LỖI KHỞI TẠO GOOGLE DRIVE SERVICE");
            throw;
        }
    }

    private async Task<DriveService> GetDriveService(string credentialsPath, string tokenPath)
    {
        GoogleClientSecrets clientSecrets;
        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            clientSecrets = GoogleClientSecrets.FromStream(stream);
        }

        TokenResponse token;
        if (File.Exists(tokenPath))
        {
            _logger.LogInformation("Đã thấy token: {path}", tokenPath);
            var tokenJson = await File.ReadAllTextAsync(tokenPath);
            token = JsonSerializer.Deserialize<TokenResponse>(tokenJson) ?? new TokenResponse();
        }
        else
        {
            _logger.LogError("KHÔNG tìm thấy token.json tại {path}", tokenPath);
            _logger.LogError("Hãy chạy flow OAuth: /Home/GoToGoogleAuth để tạo token.");
            throw new FileNotFoundException("Không có token.json. Chạy /Home/GoToGoogleAuth trước.", tokenPath);
        }

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets.Secrets,
            Scopes = new[] { DriveService.Scope.Drive } // hoặc DriveService.Scope.DriveFile
        });

        var credential = new UserCredential(flow, "user-default", token);

        // Refresh nếu cần
        if (token.IsExpired(Google.Apis.Util.SystemClock.Default))
        {
            _logger.LogWarning("Access token hết hạn → refresh bằng RefreshToken...");
            var ok = await credential.RefreshTokenAsync(CancellationToken.None);
            if (!ok)
            {
                _logger.LogError("Không refresh được token (có thể RefreshToken bị thu hồi).");
                throw new Exception("Không thể refresh Google Drive token.");
            }

            // Cập nhật token.json (đảm bảo thư mục tồn tại)
            Directory.CreateDirectory(Path.GetDirectoryName(tokenPath)!);
            var newJson = JsonSerializer.Serialize(credential.Token, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(tokenPath, newJson);
            _logger.LogInformation("Đã ghi đè token.json tại {path}", tokenPath);
        }
        else
        {
            _logger.LogInformation("Access token vẫn còn hạn.");
        }

        return new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ITExam Application",
        });
    }

    // Upload file
    public async Task<string> UploadFileAsync(IFormFile file, string studentName, int examId)
    {
        try
        {
            string fileName = $"MaDe_{examId}_SV_{studentName}_{file.FileName}";

            var meta = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = new List<string> { _parentFolderId }
            };

            FilesResource.CreateMediaUpload req;
            using (var stream = file.OpenReadStream())
            {
                req = _service.Files.Create(meta, stream, file.ContentType);
                req.Fields = "id";
                var progress = await req.UploadAsync();
                if (progress.Status == UploadStatus.Failed)
                    throw new Exception($"Upload Google Drive lỗi: {progress.Exception?.Message}", progress.Exception);
            }

            var uploaded = req.ResponseBody;
            if (uploaded == null) throw new Exception("Google Drive không trả về ResponseBody.");
            return uploaded.Id;
        }
        catch (Exception ex)
        {
            throw new Exception($"LỖI UPLOAD GOOGLE DRIVE: {ex.Message}", ex);
        }
    }
    public async Task<string> GetFileWebViewLinkAsync(string fileId)
    {
        try
        {
            // _service đã được khởi tạo trong constructor
            var request = _service.Files.Get(fileId);

            // Yêu cầu Google API chỉ trả về trường 'webViewLink'
            request.Fields = "webViewLink";

            var file = await request.ExecuteAsync();

            // file.WebViewLink là link xem file (không phải link download)
            return file.WebViewLink;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy webViewLink cho fileId {FileId}", fileId);
            return null;
        }
    }
}
