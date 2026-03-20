using System.Net.Mail;
using MimeKit;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using Hangfire;
using NETCore.MailKit.Core;
using Microsoft.Extensions.Options;

namespace ITExam.Services
{
    public class EmailService
    {

        private readonly RoutingAPI _routingAPI;

        public EmailService(IOptions<RoutingAPI> routingOptions)
        {
            _routingAPI = routingOptions.Value;
        }

        public async Task SendAddExamNotification(string emailTo, string content, bool isBodyHtml)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ITExam HUTECH", _routingAPI.AdminEmail));
            message.To.Add(new MailboxAddress("Người nhận", emailTo));
            message.Subject = "Thông báo";
            message.Body = new TextPart(isBodyHtml ? "html" : "plain")
            {
                Text = content
            };
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_routingAPI.AdminEmail, _routingAPI.AdminEmailPass);

                await client.SendAsync(message); 

                await client.DisconnectAsync(true);
            }
        }
    }


}
