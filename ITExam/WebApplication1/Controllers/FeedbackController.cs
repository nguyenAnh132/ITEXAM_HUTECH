using Hangfire;
using ITExam.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ITExam.Controllers
{
    [Route("feedback")]
    public class FeedbackController : Controller
    {
        private readonly EmailService _emailService;
        private readonly RoutingAPI _routingAPI;

        public FeedbackController(EmailService emailService, IOptions<RoutingAPI> routingOptions)
        {
            _emailService = emailService;
            _routingAPI = routingOptions.Value;
        }

        [HttpPost("send")]
        public IActionResult SendFeedback([FromBody] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { message = "Nội dung không được để trống!" });
            }

            string html = $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, sans-serif;
                        background: #f5f7fa;
                        margin: 0;
                        padding: 0;
                    }}
                    .container {{
                        max-width: 600px;
                        margin: 30px auto;
                        background: #ffffff;
                        border-radius: 12px;
                        overflow: hidden;
                        box-shadow: 0 5px 25px rgba(0,0,0,0.08);
                    }}
                    .header {{
                        background: #0051cc;
                        padding: 20px 30px;
                        color: white;
                    }}
                    .header h2 {{
                        margin: 0;
                        font-size: 22px;
                    }}
                    .content {{
                        padding: 30px;
                        color: #333;
                        line-height: 1.7;
                    }}
                    .label {{
                        font-weight: bold;
                        margin-bottom: 6px;
                        display: block;
                        color: #0051cc;
                    }}
                    .message-box {{background: #eef3ff;
                        padding: 15px 20px;
                        border-radius: 6px;
                        white-space: pre-wrap;
                    }}
                    .footer {{
                        font-size: 13px;
                        text-align: center;
                        padding: 18px;
                        background: #f1f2f4;
                        color: #666;
                    }}
                </style>
            </head>

            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>ITExam - Góp ý từ người dùng</h2>
                    </div>

                    <div class='content'>
                        <span class='label'>Nội dung góp ý:</span>
                        <div class='message-box'>
                            {content}
                        </div>
                    </div>

                    <div class='footer'>
                        ITExam - Hệ thống tạo đề thi, bài kiểm tra
                    </div>
                </div>
            </body>
            </html>";
            BackgroundJob.Enqueue(() =>
                _emailService.SendAddExamNotification(_routingAPI.AdminEmail, html, true)
            );

            return Ok(new { message = "Gửi thành công!" });
        }
    }

}
