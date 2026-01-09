using Microsoft.EntityFrameworkCore;
using PACOM.WebhookApp.Data;

namespace PACOM.WebhookApp.Service
{
    public class EmailConfigurationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailConfigurationService> _logger;
        private EmailConfiguration? _cachedConfiguration;
        private DateTime _lastCacheTime = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public EmailConfigurationService(
            IServiceScopeFactory scopeFactory,
            ILogger<EmailConfigurationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<EmailConfiguration?> GetConfigurationAsync()
        {
            // Check if cache is still valid
            if (_cachedConfiguration != null && 
                DateTime.UtcNow - _lastCacheTime < _cacheExpiration)
            {
                return _cachedConfiguration;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get the first (and should be only) email configuration
                var config = await context.EmailConfigurations
                    .FirstOrDefaultAsync();

                if (config != null)
                {
                    _cachedConfiguration = config;
                    _lastCacheTime = DateTime.UtcNow;
                    _logger.LogInformation("Email configuration loaded from database");
                }
                else
                {
                    _logger.LogWarning("No email configuration found in database");
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading email configuration from database");
                return null;
            }
        }

        public async Task<EmailConfiguration> SaveConfigurationAsync(EmailConfiguration config)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existing = await context.EmailConfigurations.FirstOrDefaultAsync();

            if (existing != null)
            {
                // Update existing configuration
                existing.SmtpServer = config.SmtpServer;
                existing.SmtpPort = config.SmtpPort;
                existing.SenderEmail = config.SenderEmail;
                existing.SenderName = config.SenderName;
                existing.Username = config.Username;
                existing.Password = config.Password;
                existing.EnableSsl = config.EnableSsl;
                
                context.EmailConfigurations.Update(existing);
            }
            else
            {
                // Create new configuration
                context.EmailConfigurations.Add(config);
            }

            await context.SaveChangesAsync();

            // Clear cache to force reload
            _cachedConfiguration = null;
            _lastCacheTime = DateTime.MinValue;

            _logger.LogInformation("Email configuration saved to database");
            return config;
        }

        public async Task<bool> TestConfigurationAsync(EmailConfiguration config)
        {
            try
            {
                using var smtpClient = new System.Net.Mail.SmtpClient(config.SmtpServer, config.SmtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(config.Username, config.Password),
                    EnableSsl = config.EnableSsl
                };

                // Just test the connection without sending
                await Task.Run(() => smtpClient.Send(
                    new System.Net.Mail.MailMessage(
                        config.SenderEmail, 
                        config.SenderEmail, 
                        "Test Connection", 
                        "This is a test email to verify SMTP configuration."
                    )
                ));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email configuration test failed");
                return false;
            }
        }

        public void ClearCache()
        {
            _cachedConfiguration = null;
            _lastCacheTime = DateTime.MinValue;
        }
    }
}