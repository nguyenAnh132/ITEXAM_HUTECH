using System.Net.Mail;
using MimeKit;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using Hangfire;
using NETCore.MailKit.Core;

namespace ITExam.Services
{
    public class EmailService
    {
        public static string adminEmail = "example@gmail.com";
        public async Task SendAddExamNotification(string emailTo, string content, bool isBodyHtml)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ITExam HUTECH", adminEmail));
            message.To.Add(new MailboxAddress("Người nhận", emailTo));
            message.Subject = "Thông báo";
            message.Body = new TextPart(isBodyHtml ? "html" : "plain")
            {
                Text = content
            };
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(adminEmail, "example");

                await client.SendAsync(message); 

                await client.DisconnectAsync(true);
            }
        }
    }


}
