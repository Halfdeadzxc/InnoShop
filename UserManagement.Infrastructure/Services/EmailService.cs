using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserManagement.Application.Interfaces;

namespace UserManagement.Infrastructure.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendConfirmationEmailAsync(string email, string token, CancellationToken cancellationToken = default)
        {
            var confirmationLink = $"https://localhost:5000/api/auth/confirm-email?token={WebUtility.UrlEncode(token)}";

            var subject = "Confirm Your Email - InnoShop";
            var body = $"""
                <html>
                <body>
                    <h2>Welcome to InnoShop!</h2>
                    <p>Please confirm your email address by clicking the link below:</p>
                    <p><a href="{confirmationLink}" style="background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;">Confirm Email</a></p>
                    <p>Or copy and paste this link in your browser:</p>
                    <p>{confirmationLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <br/>
                    <p>If you didn't create an account, please ignore this email.</p>
                </body>
                </html>
                """;

            await SendEmailAsync(email, subject, body, cancellationToken);
        }

        public async Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken = default)
        {
            var resetLink = $"https://localhost:5000/api/auth/reset-password?token={WebUtility.UrlEncode(token)}";

            var subject = "Reset Your Password - InnoShop";
            var body = $"""
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>You requested to reset your password. Click the link below to set a new password:</p>
                    <p><a href="{resetLink}" style="background-color: #f44336; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;">Reset Password</a></p>
                    <p>Or copy and paste this link in your browser:</p>
                    <p>{resetLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <br/>
                    <p>If you didn't request a password reset, please ignore this email.</p>
                </body>
                </html>
                """;

            await SendEmailAsync(email, subject, body, cancellationToken);
        }

        public async Task SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default)
        {
            var subject = "Welcome to InnoShop!";
            var body = $"""
                <html>
                <body>
                    <h2>Welcome to InnoShop, {firstName}!</h2>
                    <p>Your account has been successfully created and is ready to use.</p>
                    <p>Start exploring our products and enjoy your shopping experience!</p>
                    <br/>
                    <p>Happy shopping,<br/>The InnoShop Team</p>
                </body>
                </html>
                """;

            await SendEmailAsync(email, subject, body, cancellationToken);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var message = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                await client.SendMailAsync(message, cancellationToken);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw new ApplicationException($"Failed to send email: {ex.Message}");
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password),
                EnableSsl = _emailSettings.EnableSsl,
                Timeout = 30000
            };
        }
    }
}