using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Web.Models;

namespace Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IWebHostEnvironment _env;

        public EmailService(IOptions<EmailSettings> emailSettings, IWebHostEnvironment env)
        {
            _emailSettings = emailSettings.Value;
            _env = env;
        }

        public Task SendEmailAsync(string to, string subject, string message)
        {
            if (_env.IsDevelopment())
            {
                ServicePointManager.ServerCertificateValidationCallback =
                    (object s, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true;
            }

            var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPass),
                EnableSsl = true,
                Timeout = 10000 // Adiciona um timeout de 10 segundos
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(to);

            return client.SendMailAsync(mailMessage);
        }
    }
}