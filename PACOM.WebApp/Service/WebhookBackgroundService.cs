using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PACOM.WebApp.Model;
using PACOM.WebApp.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;


namespace PACOM.WebApp.Service
{
    public class WebhookBackgroundService : BackgroundService
    {
        private readonly WebhookSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _connectionString;

        public WebhookBackgroundService(IOptions<WebhookSettings> settings,
                                        IHttpClientFactory httpClientFactory,
                                        IConfiguration config)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("🚀 Webhook background sender started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_settings.Enabled)
                {
                    Console.WriteLine("⚙️ Webhook sending disabled — sleeping...");
                    await Task.Delay(_settings.CheckIntervalSeconds * 1000, stoppingToken);
                    continue;
                }


                if (!await IsWebhookUrlValid(_settings.WebhookLink))
                {
                    Console.WriteLine("🚫 Webhook URL invalid or unreachable — sleeping...");
                    await Task.Delay(_settings.CheckIntervalSeconds * 1000, stoppingToken);
                    continue;
                }

                await ProcessUnsentRecords(stoppingToken);

                await Task.Delay(_settings.CheckIntervalSeconds * 1000, stoppingToken);
            }
        }

        private async Task ProcessUnsentRecords(CancellationToken token)
        {
            try
            {
                EventLogModel? LatestRecords = DatasourcesService.GetLatestEvent(_settings.OrganizationCode).Data;

                List<EventLogModel> UnsentEvent = DatasourcesService.GetEvent(_settings.OrganizationCode, LatestRecords.MalaysiaTime, DateTime.Now).Data.AsList();

                if (!UnsentEvent.Any())
                {
                    Console.WriteLine("🔍 No new unsent records.");
                    return;
                }

                foreach (var record in UnsentEvent)
                {
                    bool sent = await SendWebhookAsync(record);
                    if (sent)
                    {
                        Console.WriteLine($"✅ Sent record ID {record.Id}");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing records: {ex.Message}");
            }
        }

        private async Task<bool> SendWebhookAsync(EventLogModel record)
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(record);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(_settings.WebhookLink, content);
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

        private async Task<bool> IsWebhookUrlValid(string url)
        {
            var client = _httpClientFactory.CreateClient();
            try
            {

                // Use HEAD to test reachability without sending data
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await client.SendAsync(request);

                // Some APIs may not support HEAD properly — treat 405 (Method Not Allowed) as valid
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                    return true;

                return false;

            }
            catch(Exception ex)
            {
                Console.WriteLine($"❌ Error processing records: {ex.Message}");
                return false;
            }
        }

    }
}
