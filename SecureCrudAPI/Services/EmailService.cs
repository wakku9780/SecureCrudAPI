using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Net.Mail;

namespace SecureCrudAPI.Services
{
    public class EmailService
    {

        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)

        {
            var emailSettings = _configuration.GetSection("EmailSettings");


            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Admin", emailSettings["SenderEmail"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true; // Ignore SSL validation

                    await client.ConnectAsync(emailSettings["SMTPServer"], int.Parse(emailSettings["SMTPPort"]), SecureSocketOptions.Auto);
                    client.CheckCertificateRevocation = false; // Skip revocation check
                    await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["SenderPassword"]);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email sending failed: {ex.Message}");
                }
            }


        }
    }
}

