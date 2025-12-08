using Microsoft.Extensions.Options;
using PACOM.WebhookApp.Model;
using System.Text;
using System.Text.Json;

namespace PACOM.WebhookApp.Service
{
    public class WebhookSenderService
    {
        private readonly WebhookSettings _settings;
        private readonly HttpClient _httpClient;

        public WebhookSenderService(IOptions<WebhookSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<bool> SendWebhookAsync(object payload)
        {
            if (!_settings.Enabled)
            {
                Console.WriteLine("🚫 Webhook sending is disabled in configuration.");
                return false;
            }

            if (!await IsWebhookUrlValid(_settings.WebhookLink))
            {
                Console.WriteLine("🚫 Webhook URL invalid or unreachable.");
                return false;
            }

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            //for (int attempt = 1; attempt <= _settings.MaxRetries; attempt++)
            //{
            //    try
            //    {
            //        var response = await _httpClient.PostAsync(_settings.WebhookLink, content);
            //        var responseText = await response.Content.ReadAsStringAsync();

            //        if (response.IsSuccessStatusCode)
            //        {
            //            Console.WriteLine($"✅ Webhook sent successfully on attempt {attempt}");
            //            return true;
            //        }
            //        else
            //        {
            //            Console.WriteLine($"❌ Attempt {attempt}: {response.StatusCode} - {responseText}");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"⚠️ Attempt {attempt} failed: {ex.Message}");
            //    }

            //    await Task.Delay(_settings.RetryDelaySeconds * 1000);
            //}

            Console.WriteLine("🚫 All webhook attempts failed.");
            return false;
        }

        private async Task<bool> IsWebhookUrlValid(string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
