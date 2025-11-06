using Microsoft.AspNetCore.Mvc;
using PACOM.WebApp.Model;
using PACOM.WebApp.Service;

namespace PACOM.WebApp
{
    [Route("[controller]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        [HttpPost("EventLog")]
        public IActionResult EventLog( string OrganizationCode, DateTime StartDate, DateTime EndDate)
        {
            var result = DatasourcesService.GetEvent(OrganizationCode, StartDate, EndDate);
            return Ok(result);
        }

        [HttpGet("List User")]
        public IActionResult ListUser( string OrganizationCode)
        {
            var result = DatasourcesService.ListAllUsers(OrganizationCode);
            return Ok(result);
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            try
            {

                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                    return BadRequest("Empty request body");


                // Save record into SQL
                await DatasourcesService.SaveWebhookToDatabase(body);

                Console.WriteLine("📩 Webhook received and saved:");
                Console.WriteLine(body);

                return Ok(new { status = "Saved successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving webhook: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("WebhookSender")]
        public IActionResult UpdateSettings([FromBody] WebhookSettings newSettings)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

            var json = System.IO.File.ReadAllText(filePath);
            dynamic? jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            if (jsonObj == null)
                throw new InvalidOperationException("Failed to deserialize JSON.");

            var Url = newSettings.WebhookLink + "api/receive";

            jsonObj["WebhookSettings"]["Enabled"] = newSettings.Enabled;
            jsonObj["WebhookSettings"]["WebhookLink"] = $"{newSettings.WebhookLink}/api/receive";
            jsonObj["WebhookSettings"]["CheckIntervalSeconds"] = newSettings.CheckIntervalSeconds;
            jsonObj["WebhookSettings"]["OrganizationCode"] = newSettings.OrganizationCode;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filePath, output);
            return Ok(new { message = "Webhook settings updated at runtime" });
        }
    }
}
