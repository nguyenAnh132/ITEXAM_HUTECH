using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ITExam.Models;

namespace ITExam.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileCleanupService> _logger;

        //private readonly TimeSpan _checkPeriod = TimeSpan.FromHours(24);
        private readonly TimeSpan _checkPeriod = TimeSpan.FromMinutes(2); //Phiên bản debug

        //private readonly TimeSpan _fileAgeThreshold = TimeSpan.FromHours(24);
        private readonly TimeSpan _fileAgeThreshold = TimeSpan.FromMinutes(1); //Phiên bản debug

        public FileCleanupService(IServiceProvider serviceProvider, IWebHostEnvironment env, ILogger<FileCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FileCleanupService đã khởi động. Sẽ quét dọn định kỳ mỗi giờ.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanUpFilesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình dọn dẹp file rác.");
                }

                // Chờ đến chu kỳ tiếp theo
                await Task.Delay(_checkPeriod, stoppingToken);
            }
        }

        private async Task CleanUpFilesAsync()
        {
            _logger.LogInformation("Bắt đầu quét dọn ảnh rác trong thư mục uploads...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ITExamDbContext>();

                var questionContents = await context.QuestionBanks
                    .AsNoTracking()
                    .Select(q => q.QuestionContent)
                    .ToListAsync();

                var choiceContents = await context.QuestionBanks
                    .AsNoTracking()
                    .Where(q => q.ChoiceContent != null)
                    .Select(q => q.ChoiceContent)
                    .ToListAsync();

                // Nếu sinh viên làm bài tự luận có chèn ảnh thì uncomment dòng dưới
                /*
                var studentAnswers = await context.StudentAnswers
                    .AsNoTracking()
                    .Where(a => a.Content != null) // Giả sử model StudentAnswer có trường Content
                    .Select(a => a.Content)
                    .ToListAsync();
                */

                var allHtmlContent = new List<string>();
                allHtmlContent.AddRange(questionContents);
                allHtmlContent.AddRange(choiceContents);
                // allHtmlContent.AddRange(studentAnswers); // Uncomment nếu dùng

                var usedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var regex = new Regex(@"/uploads/([\w-]+\.(png|jpg|jpeg|gif|webp|bmp|heic|heif))", RegexOptions.IgnoreCase);

                foreach (var html in allHtmlContent)
                {
                    if (string.IsNullOrWhiteSpace(html)) continue;

                    var matches = regex.Matches(html);
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            usedFiles.Add(match.Groups[1].Value);
                        }
                    }
                }

                _logger.LogInformation($"Tìm thấy {usedFiles.Count} ảnh đang được sử dụng trong Database.");

                var uploadsPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    _logger.LogWarning("Thư mục uploads không tồn tại, bỏ qua quét dọn.");
                    return;
                }

                var filesOnDisk = Directory.GetFiles(uploadsPath);
                int deletedCount = 0;
                long freedSpace = 0;

                foreach (var filePath in filesOnDisk)
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileInfo = new FileInfo(filePath);

                    if (!usedFiles.Contains(fileName) && fileInfo.CreationTime < DateTime.Now.Subtract(_fileAgeThreshold))
                    {
                        try
                        {
                            long size = fileInfo.Length;
                            File.Delete(filePath);
                            deletedCount++;
                            freedSpace += size;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Không thể xóa file: {fileName}. Lỗi: {ex.Message}");
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    double mbFreed = freedSpace / 1024.0 / 1024.0;
                    _logger.LogInformation($"HOÀN TẤT DỌN DẸP: Đã xóa {deletedCount} file rác. Giải phóng {mbFreed:F2} MB.");
                }
                else
                {
                    _logger.LogInformation("Hệ thống sạch sẽ, không có file rác nào cần xóa.");
                }
            }
        }
    }
}