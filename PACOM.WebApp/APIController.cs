using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PACOM.WebApp.Data;
using PACOM.WebApp.Model;
using PACOM.WebApp.Models;
using PACOM.WebApp.Service;

namespace PACOM.WebApp
{
    [Route("[controller]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly DatasourcesService _datasourcesService;

        // ✅ Constructor receives it via dependency injection
        public APIController(DatasourcesService datasourcesService)
        {
            _datasourcesService = datasourcesService;
        }

        [HttpPost("AddUpdateOrganization")]
        public async Task<IActionResult> AddUpdateOrganization([FromBody] Organization organization)
        {
            if (organization == null)
                return BadRequest(new { Message = "Invalid organization data." });

            var result = await _datasourcesService.ManageOrganizationAsync(organization);

            if (result.Error == 0)
                return Ok(result);
            else
                return StatusCode(500, result);
        }


        [HttpGet("ListOrganizations")]
        public async Task<IActionResult> GetOrganization()
        {
            var response = new PacomResponse<List<Organization>>();

            try
            {
                // Find organization by token
                var organization = await _datasourcesService.ListOrganizationAsync();

                if (organization.Data == null)
                {
                    response.Error = 1;
                    response.Message = "Organization not found.";
                    response.Data = null;
                    return Unauthorized(response);
                }

                // Build success response
                response.Error = 0;
                response.Message = "Organization retrieved successfully.";
                response.Data = organization.Data;

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Error = 1;
                response.Message = $"Error retrieving organization: {ex.Message}";
                response.Data = null;
                return StatusCode(500, response);
            }
        }


        [HttpGet("ListPacomOrganization")]
        public IActionResult PacomListOrganization()
        {
            var result = DatasourcesService.ListPacomOrganization();
            return Ok(result);
        }


        [HttpPost("EventLog")]
        public IActionResult PacomEventLog( string OrganizationCode, DateTime StartDate, DateTime EndDate)
        {
            // Define Malaysia time zone (same as Singapore)
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

            // Convert the received local times to UTC
            DateTime startUtc = TimeZoneInfo.ConvertTimeToUtc(StartDate, malaysiaTimeZone);
            DateTime endUtc = TimeZoneInfo.ConvertTimeToUtc(EndDate, malaysiaTimeZone);

            var result = DatasourcesService.GetEvent(startUtc, endUtc, OrganizationCode);
            return Ok(result);
        }


        [HttpPost("Webhhok")]
        public async Task<IActionResult> PacomWebhook( string OrganizationCode, bool Actived, string WebhookUrl)
        {
            var Tenant = await _datasourcesService.ListOrganizationAsync();

            if (!Tenant.Data!.Any(x => x.Code == OrganizationCode))
                return BadRequest(new { Message = "Invalid organization data." });

            var org = new Organization
            {
                Code = OrganizationCode,
                IsActive = Actived,
                Name = Tenant.Data.First(x => x.Code == OrganizationCode).Name,
                Description = Tenant.Data.First(x => x.Code == OrganizationCode).Description,
                url = WebhookUrl

            };

            var result = await _datasourcesService.ManageOrganizationAsync(org);

            if (result.Error == 0)
                return Ok(result);
            else
                return StatusCode(500, result);
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
                    await DatasourcesService.SaveWebhookInTAMS(acstrx);
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
