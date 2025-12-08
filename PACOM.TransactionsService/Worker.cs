using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PACOM.WebhookApp.Data;
using PACOM.WebhookApp.Model;
using PACOM.WebhookApp.Service;
using System.Text;
using System.Text.Json;

namespace PACOM.TransactionsService
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
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker service starting at {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                //if (_logger.IsEnabled(LogLevel.Information))
                //{
                //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //}

                await ProcessCurrentRecords(stoppingToken);
                await Task.Delay(_settings.CheckIntervalSeconds * 1000, stoppingToken);
            }
        }

        private async Task ProcessCurrentRecords(CancellationToken token)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _datasourcesService = scope.ServiceProvider.GetRequiredService<DatasourcesService>();

                // Get the latest processed record from PACOM by Version
                EventLogModel? latestResponse = DatasourcesService.GetLatestPacomEvent().Data;

                // Get last processed ActivityEvent by Version.
                ActivityEvent? lastProcess = _datasourcesService.LastActivityEventAsync().Result.Data;

                string LastPrcoessVersion = string.Empty;

                if (lastProcess == null)
                {
                    EventLogModel? FirstRecordPACOM = DatasourcesService.GetFirstPacomEvent().Data;
                    if (FirstRecordPACOM == null)
                    {
                        //Console.WriteLine("🔍 No records in PACOM to process.");
                        _logger.LogInformation("No records in PACOM to process.");
                        return;
                    }
                    else
                    {
                        LastPrcoessVersion = FirstRecordPACOM.Version;
                    }
                }
                else
                {
                    LastPrcoessVersion = lastProcess.Version;
                }


                // Fetch unsent records from PACOM.DBO.EventLogModel table
                List<EventLogModel> unsentRecords = DatasourcesService.GetEventByVersion(LastPrcoessVersion, latestResponse?.Version ?? "")?.Data ?? new List<EventLogModel>();

                if (!(unsentRecords?.Any() ?? false))
                {
                    //Resent failed record
                    //await ResentFailedRecords();

                    //Console.WriteLine("🔍 No new unsent records.");
                    _logger.LogInformation("No new unsent records.");
                    return;
                }

                // Map EventLogModel to ActivityEvent
                List<ActivityEvent> log = unsentRecords.Select(r => new ActivityEvent
                {
                    Version = r.Version,
                    Id = r.Id,
                    Scope = r.Scope,
                    ScopeName = r.ScopeName,
                    Organization = r.OrganizationName,
                    EventId = r.EventId,
                    EventName = r.EventName,
                    UserId = r.UserId,
                    UserName = r.UserName,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    CredentialId = r.CredentialId,
                    CredentialNumber = r.CredentialNumber,
                    Value = r.Value,
                    AreaFromId = r.AreaFromId,
                    AreaToId = r.AreaToId,
                    CustomDataUDF = r.CustomDataUDF,
                    CustomDataString = r.CustomDataString,
                    UtcTime = r.UtcTime,
                    ReaderName = r.ReaderName,
                    CustomDataEventType = System.Text.Json.JsonSerializer.Serialize(r.CustomDataEventType),
                    CustomDataUDFType = System.Text.Json.JsonSerializer.Serialize(r.CustomDataUDFType),
                    MykadNumber = r.MykadNumber,
                    IsProcessed = false
                }).ToList();

                // direct save without sent webhook.
                foreach (var record in log)
                {
                    await _datasourcesService.StoreActivityEventAsync(record);
                }

                // direct save without sent webhook when return failed.
                //if (!await ManageWebhookOrganization(log))
                //{
                //    foreach (var record in log)
                //    {
                //        var result = _datasourcesService.StoreActivityEventAsync(record);
                //    }

                //}

                //Resent failed record
                //await ResentFailedRecords();

            }
            catch (Exception ex)
            {
                //Console.WriteLine($"❌ Error processing records: {ex.Message}");
                _logger.LogError($"❌ Error processing records: {ex.Message}");
            }
        }

        private async Task ResentFailedRecords()
        {
            using var scope = _scopeFactory.CreateScope();
            var _datasourcesService = scope.ServiceProvider.GetRequiredService<DatasourcesService>();

            // Fetch unsent records from PACOM.DBO.EventLogModel table
            List<ActivityEvent>? FailedSendRecords = _datasourcesService.GetUnprocessEventAsync().Result.Data;

            if (!(FailedSendRecords?.Any() ?? false))
            {
                //Console.WriteLine("🔍 No new unsent records.");
                _logger.LogInformation("No new unsent records.");
                return;
            }

            await ManageWebhookOrganization(FailedSendRecords);
        }

        private async Task<bool> ManageWebhookOrganization(List<ActivityEvent> log)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _datasourcesService = scope.ServiceProvider.GetRequiredService<DatasourcesService>();

                // Get all organizations registered for webhooks
                List<Organization>? organizations = _datasourcesService.ListOrganizationAsync().Result.Data;

                if (organizations == null || !organizations.Any())
                {
                    //Console.WriteLine($"ℹ️ No records for organization.");
                    _logger.LogInformation("No records for organization.");
                    return false;
                }

                foreach (var site in organizations)
                {
                    // Filter records for the current organization
                    var siteRecords = log.Where(r => r.Organization == site.Name).ToList();

                    if (!siteRecords.Any())
                    {
                        //Console.WriteLine($"ℹ️ No records for organization {site.Name}.");
                        _logger.LogInformation($"No records for organization {site.Name}.");
                        continue;
                    }

                    foreach (var record in siteRecords)
                    {
                        // Send webhook if organization is active and URL is valid
                        if (site.IsActive == true)
                        {
                            bool sent = await SendWebhookAsync(record, site);
                            if (sent)
                            {
                                record.IsProcessed = true;
                                //Console.WriteLine($"✅ Sent record ID {record.Id}");
                            }

                            // Store & update record event only active webhook
                            var results = _datasourcesService.StoreActivityEventAsync(record);

                        }
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"⚠️ Attempt error: {ex.Message}");
                _logger.LogError($"Attempt error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendWebhookAsync(ActivityEvent record, Organization organization)
        {
            var client = _httpClientFactory.CreateClient();
            string json = System.Text.Json.JsonSerializer.Serialize(record);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(organization.url, content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                //Console.WriteLine($"⚠️ Attempt failed: {response.StatusCode}");
                _logger.LogError($"Attempt failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"⚠️ Attempt error: {ex.Message}");
                _logger.LogError($"Attempt error: {ex.Message}");

            }
            //Console.WriteLine($"🚫 All attempts failed for record ID {record.Id}");
            _logger.LogError($"All attempts failed for record ID {record.Id}");
            return false;
        }

    }
}
