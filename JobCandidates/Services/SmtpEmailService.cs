using System.Net;
using System.Net.Mail;

namespace JobCandidates.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var host = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var fromEmail = _config["Smtp:FromEmail"];
            var fromName = _config["Smtp:FromName"] ?? "JobCandidates";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("SMTP configuration is missing.");
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var subject = "Your JobCandidates OTP Code";
            var body = $@"
                <html>
                <body>
                    <h3>JobCandidates Email Verification</h3>
                    <p>Your OTP code is:</p>
                    <h2 style='color:#0d6efd;'>{otpCode}</h2>
                    <p>This code expires in 10 minutes.</p>
                    <p>If you did not request this code, you can ignore this email.</p>
                </body>
                </html>";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}