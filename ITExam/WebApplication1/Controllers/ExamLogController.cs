using ITExam.ExternalModels.Subject;
using ITExam.Models;
using ITExam.Services;
using Microsoft.AspNetCore.Mvc;

namespace ITExam.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    public class ExamLogController : ControllerBase
    {
        private readonly RedisService _redis;

        public ExamLogController(RedisService redis)
        {
            _redis = redis;
        }

        [HttpGet("{examId}/{classId}/{studentId}")]
        public async Task<IActionResult> GetStudentLogs(string examId, string classId, string studentId)
        {
            if (string.IsNullOrEmpty(examId) || string.IsNullOrEmpty(studentId))
            {
                return BadRequest("Missing examId or studentId.");
            }

            var redisKey = $"ITExam_logs:{examId}:{classId}:{studentId}";
            var logs = await _redis.GetLogsAsync(redisKey);

            if (logs == null || logs.Count == 0)
            {
                return NotFound($"Logs for student {studentId} in exam {examId} not found.");
            }

            return Ok(logs);
        }


        [HttpGet("structure/{examId}/{classId}/{studentId}")]
        public async Task<IActionResult> GetExamStructure(string examId, string classId, string studentId)
        {
            var key = $"ITExam_examStructure:{examId}:{classId}:{studentId}";
            var structureJson = await _redis.GetStringAsync(key);

            if (string.IsNullOrEmpty(structureJson))
            {
                return NotFound("Không tìm thấy dữ liệu đề thi.");
            }

            try
            {
                var examStructure = System.Text.Json.JsonSerializer.Deserialize<List<ExamStructureDto>>(structureJson, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(examStructure);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi phân tích chuỗi JSON: {ex.Message}");
                return StatusCode(500, "Lỗi khi xử lý dữ liệu đề thi từ Redis.");
            }
        }

        [HttpGet("info/{studentId}")]
        public async Task<IActionResult> GetClientInfo(string studentId)
        {
            var ip = await _redis.GetStringAsync($"ITExam_ip:{studentId}") ?? "Chưa có";
            var ua = await _redis.GetStringAsync($"ITExam_ua:{studentId}") ?? "Chưa có";

            return Ok(new
            {
                studentId = studentId,
                ip = ip,
                userAgent = ua
            });
        }

    }
}