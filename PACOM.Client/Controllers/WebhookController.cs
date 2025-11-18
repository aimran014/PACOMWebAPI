using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using PACOM.Client.Data;
using PACOM.Client.Model;

namespace PACOM.WebhookClient
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {

        private static string? _connectionString;

        public WebhookController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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

                // Deserialize JSON into model
                var webhookData = JsonConvert.DeserializeObject<ActivityEvent>(body);

                if (webhookData == null)
                    return BadRequest("Invalid JSON structure");

                AcstrxModel acstrx = new AcstrxModel
                {
                    DateTrx = TimeZoneInfo.ConvertTimeFromUtc(webhookData.UtcTime, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time")),
                    BadgeNo = webhookData.MykadNumber,
                    ReaderNo = webhookData.ReaderName,
                    flag = 0,
                    Trx_Type = 0
                };


                if (!string.IsNullOrWhiteSpace(acstrx.BadgeNo))
                {
                    // Save to TAMS database
                    using var connection = new SqlConnection(_connectionString);

                    string query = @"INSERT INTO [TAMS_ALPHA_JPM].[dbo].[ACSTRX] (DateTrx, BadgeNo, ReaderNo, flag, Trx_Type) VALUES(@DateTrx, @BadgeNo, @ReaderNo, @flag, @Trx_Type)";

                    await connection.ExecuteAsync(query, new { DateTrx = acstrx.DateTrx, BadgeNo = acstrx.BadgeNo, ReaderNo = acstrx.ReaderNo, flag = acstrx.flag, Trx_Type = acstrx.Trx_Type });

                }

                return Ok(new { status = "Saved successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving webhook: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
