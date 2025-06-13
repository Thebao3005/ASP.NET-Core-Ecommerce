using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceMVC.Helpers
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendEmail(string to, string subject, string body)
        {
            // Gửi email qua SMTP
            var smtpClient = new SmtpClient(_configuration["Smtp:Server"])
            {
                Port = int.Parse(_configuration["Smtp:Port"]),
                Credentials = new NetworkCredential(
                    _configuration["Smtp:Username"],
                    _configuration["Smtp:Password"]
                ),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Smtp:From"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);
            smtpClient.Send(mailMessage);
        }
        public void SendReplyEmail(string to, string replyMessage)
        {
            var smtpSettings = _configuration.GetSection("Smtp");

            using (var smtpClient = new SmtpClient(smtpSettings["Server"]))
            {
                smtpClient.Port = int.Parse(smtpSettings["Port"]);
                smtpClient.Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]);
                smtpClient.EnableSsl = true;

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(smtpSettings["From"]);
                    mailMessage.To.Add(to);
                    mailMessage.Subject = "Phản hồi từ ADMIN WEBMUAHANGCHUNG";
                    mailMessage.Body = $@"
                <h3>Xin chào!</h3>
                <p>Chúng tôi đã nhận được góp ý của bạn và đây là phản hồi từ đội ngũ quản trị:</p>
                <blockquote style='border-left: 4px solid #007bff; padding-left: 10px; color: #333;'>{replyMessage}</blockquote>
                <p>Cảm ơn bạn đã đóng góp ý kiến. Nếu có thêm thắc mắc, vui lòng liên hệ lại!</p>
                <br>
                <p>Trân trọng,</p>
                <p><b>ADMIN WEBMUAHANGCHUNG</b></p>
            ";
                    mailMessage.IsBodyHtml = true;

                    smtpClient.Send(mailMessage);
                }
            }
        }

    }
}

