using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace EmoTagger.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendResetPasswordEmail(string toEmail, string resetLink)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            var smtpPass = _configuration["EmailSettings:SmtpPass"];

            var fromAddress = new MailAddress(smtpUser, "Muzik Support");
            var toAddress = new MailAddress(toEmail);

            using (var smtp = new SmtpClient(smtpHost, smtpPort))
            {
                smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);
                smtp.EnableSsl = true;

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Reset Your Password",
                    Body = $"Click <a href='{resetLink}'>here</a> to reset your password.",
                    IsBodyHtml = true
                })
                {
                    await smtp.SendMailAsync(message);
                }
            }
        }
    }
}
