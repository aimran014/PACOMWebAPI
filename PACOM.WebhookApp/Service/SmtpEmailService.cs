using Microsoft.AspNetCore.Identity;
using PACOM.WebhookApp.Data;
using System.Net;
using System.Net.Mail;

namespace PACOM.WebhookApp.Service
{
    public class SmtpEmailService : IEmailSender<ApplicationUser>
    {
        private readonly EmailConfigurationService _emailConfigService;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            EmailConfigurationService emailConfigService,
            ILogger<SmtpEmailService> logger)
        {
            _emailConfigService = emailConfigService;
            _logger = logger;
        }

        public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            var subject = "Confirm your email";
            var htmlMessage = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Welcome to PACOM Webhook System!</h2>
                    <p>Hello {user.UserName},</p>
                    <p>Please confirm your account by clicking the button below:</p>
                    <p style='margin: 20px 0;'>
                        <a href='{confirmationLink}' 
                           style='background-color: #4CAF50; color: white; padding: 12px 24px; 
                                  text-decoration: none; border-radius: 4px; display: inline-block;'>
                            Confirm Email
                        </a>
                    </p>
                    <p>If the button doesn't work, copy and paste this link into your browser:</p>
                    <p style='color: #666; word-break: break-all;'>{confirmationLink}</p>
                    <hr style='margin: 20px 0; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #999; font-size: 12px;'>
                        If you didn't create an account, please ignore this email.
                    </p>
                </body>
                </html>";

            await SendEmailAsync(email, subject, htmlMessage);
        }

        public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            var subject = "Reset your password";
            var htmlMessage = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Password Reset Request</h2>
                    <p>Hello {user.UserName},</p>
                    <p>You requested to reset your password. Click the button below to proceed:</p>
                    <p style='margin: 20px 0;'>
                        <a href='{resetLink}' 
                           style='background-color: #2196F3; color: white; padding: 12px 24px; 
                                  text-decoration: none; border-radius: 4px; display: inline-block;'>
                            Reset Password
                        </a>
                    </p>
                    <p>If the button doesn't work, copy and paste this link into your browser:</p>
                    <p style='color: #666; word-break: break-all;'>{resetLink}</p>
                    <hr style='margin: 20px 0; border: none; border-top: 1px solid #ddd;'>
                    <p style='color: #999; font-size: 12px;'>
                        If you didn't request a password reset, please ignore this email or contact support if you have concerns.
                    </p>
                </body>
                </html>";

            await SendEmailAsync(email, subject, htmlMessage);
        }

        public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            var subject = "Your password reset code";
            var htmlMessage = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Password Reset Code</h2>
                    <p>Hello {user.UserName},</p>
                    <p>Your password reset code is:</p>
                    <div style='background-color: #f5f5f5; padding: 15px; margin: 20px 0; 
                                border-radius: 4px; font-size: 24px; font-weight: bold; 
                                text-align: center; letter-spacing: 2px;'>
                        {resetCode}
                    </div>
                    <p>Enter this code on the password reset page to continue.</p>
                    <p style='color: #999; font-size: 12px;'>This code will expire in 15 minutes.</p>
                </body>
                </html>";

            await SendEmailAsync(email, subject, htmlMessage);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var config = await _emailConfigService.GetConfigurationAsync();

            if (config == null)
            {
                _logger.LogError("Email configuration not found in database. Cannot send email.");
                throw new InvalidOperationException("Email configuration is not set up. Please configure email settings in the system.");
            }

            try
            {
                using var smtpClient = new SmtpClient(config.SmtpServer, config.SmtpPort)
                {
                    Credentials = new NetworkCredential(config.Username, config.Password),
                    EnableSsl = config.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(config.SenderEmail, config.SenderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}