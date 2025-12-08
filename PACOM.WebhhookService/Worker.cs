using Azure;
using Dapper;
using Microsoft.Extensions.Options;
using PACOM.WebhookApp.Data;
using PACOM.WebhookApp.Model;
using PACOM.WebhookApp.Service;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PACOM.WebhhookService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WebhookSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _connectionString;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(ILogger<Worker> logger, 
                      IOptions<WebhookSettings> settings,
                      IHttpClientFactory httpClientFactory,
                      IConfiguration config,
                      IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _connectionString = config.GetConnectionString("DefaultConnection");
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                // 1. Get List Organization registered.
                List<Organization> organizations = await GetOrganizations();
                if (organizations is null || organizations.Count == 0)
                {
                    _logger.LogInformation("No organizations found to process. Waiting before next check...");
                    await Task.Delay(1000, stoppingToken); // wait before retry
                    continue; // continue the loop instead of exiting
                }

                // 2. Load logs
                var NewLog = await NewProcessRecords();
                var FailedLog = await FailedProcessRecords();

                var allTasks = new List<Task>();

                foreach (var site in organizations.Where(o => o.IsActive))
                {
                    // Get logs per organization
                    var newSiteLogs = NewLog
                        .Where(o => o.Organization == site.Name)
                        .ToList();

                    var failedSiteLogs = FailedLog
                        .Where(o => o.Organization == site.Name)
                        .ToList();

                    // ✅ RUN BOTH SIMULTANEOUSLY
                    if (newSiteLogs.Any())
                    {
                        allTasks.Add(Task.Run(async () =>
                        {
                            foreach (var log in newSiteLogs)
                                await ProcessRecord(log, site, stoppingToken);
                        }, stoppingToken));
                    }

                    if (failedSiteLogs.Any())
                    {
                        allTasks.Add(Task.Run(async () =>
                        {
                            foreach (var log in failedSiteLogs)
                                await ProcessRecord(log, site, stoppingToken);
                        }, stoppingToken));
                    }
                }

                // ✅ WAIT FOR ALL ORGS + NEW + FAILED TO FINISH
                await Task.WhenAll(allTasks);

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessRecord(ActivityEvent record, Organization organization, CancellationToken token)
        {

            bool success = await SendWebhookAsync(record, organization);

            if (success)
            {
                // Mark record as processed
                //_logger.LogInformation("✅ Record {Id} sent successfully", record.Id);
                record.IsProcessed = true;
            }
            else
            {
                // Mark record as failed
                //_logger.LogWarning("⚠️ Record {Id} failed to send", record.Id);
                record.IsProcessed = false;
            }

            using var scope = _scopeFactory.CreateScope();
            var _datasourcesService = scope.ServiceProvider.GetRequiredService<DatasourcesService>();

            // Store & update record event only active webhook
            await _datasourcesService.StoreActivityEventAsync(record);
        }

        private async Task<bool> SendWebhookAsync(ActivityEvent record, Organization organization)
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(record);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(organization.url, content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                Console.WriteLine($"⚠️ Attempt failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Attempt error: {ex.Message}");
            }
            Console.WriteLine($"🚫 All attempts failed for record ID {record.Id}");
            return false;
        }

        private async Task<List<ActivityEvent>> NewProcessRecords()
        {
            using var scope = _scopeFactory.CreateScope();
            var _datasourcesService = scope.ServiceProvider.GetRequiredService<DatasourcesService>();

            var records = (await _datasourcesService.GetUnprocessEventAsync()).Data.AsList();
            return records.Where(r => r.IsProcessed == null).ToList();
        }

        private async Task<List<ActivityEvent>> FailedProcessRecords()
        {
            using var scope = _scopeFactory.CreateScope();
            var _datasourcesService = scope.ServiceProvider.GetRequiredService<DatasourcesService>();

            var records = (await _datasourcesService.GetUnprocessEventAsync()).Data.AsList();
            return records.Where(r => r.IsProcessed == false).ToList();
        }

        private async Task<List<Organization>> GetOrganizations()
        {
            using var scope = _scopeFactory.CreateScope();
            var _datasourcesService = scope.ServiceProvider.GetRequiredService<DatasourcesService>();

            // Get all organizations registered for webhooks
            List<Organization>? organizations = (await _datasourcesService.ListOrganizationAsync()).Data.AsList();
            return organizations.ToList();
        }
    }
}
