using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using PACOM.WebApp.Data;
using PACOM.WebApp.Model;
using PACOM.WebApp.Models;
using PACOM.WebApp.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PACOM.WebApp
{
    [Route("[controller]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly DatasourcesService _datasourcesService;
        private readonly IConfiguration _config;

        // ✅ Constructor receives it via dependency injection
        public APIController(DatasourcesService datasourcesService, IConfiguration config)
        {
            _datasourcesService = datasourcesService;
            _config = config;
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            // Find user in DB
            var user = FakeUsers.Users
                .FirstOrDefault(x => x.Username == login.Username && x.Password == login.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid username or password" });

            // Read secret key from appsettings.json
            var key = Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey missing"));

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, user.Username ?? "User"),
            new Claim("OrganizationId", user.OrganizationId.ToString()),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(4),   // token valid 4 hours
                SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);


            return Ok(new
            {
                Username = user.Username,
                Token = jwt,
                Expires = tokenDescriptor.Expires
            });
        }


        [HttpGet("ListPacomOrganization")]
        public IActionResult PacomListOrganization()
        {
            var result = DatasourcesService.ListPacomOrganization();
            return Ok(result);
        }


        [Authorize]
        [HttpPost("AddUpdateOrganization")]
        public async Task<IActionResult> AddUpdateOrganization([FromBody] Organization organization)
        {
            var username = User.Identity?.Name ?? "User";
            var orgId = User.FindFirst("OrganizationId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var Tenant = await _datasourcesService.ListOrganizationAsync();

            // If not admin, filter organizations by token's orgId
            if (role != "Admin")
            {
                // Find organization by token
                var code = Tenant.Data!.First(x => x.Id == int.Parse(orgId ?? "0")).Code;

                if (!Tenant.Data!.Any(x => x.Code == organization.Code))
                    return BadRequest(new { Message = "Invalid organization data." });


                if (code != organization.Code)
                {
                    return BadRequest(new { Message = "Invalid organization data." });
                }
            }

            if (organization == null)
                return BadRequest(new { Message = "Invalid organization data." });

            var result = await _datasourcesService.ManageOrganizationAsync(organization);

            if (result.Error == 0)
                return Ok(result);
            else
                return StatusCode(500, result);
        }


        [Authorize]
        [HttpGet("ListOrganizations")]
        public async Task<IActionResult> GetOrganization()
        {
            var response = new PacomResponse<List<Organization>>();

            try
            {

                var username = User.Identity?.Name ?? "User";
                var orgId = User.FindFirst("OrganizationId")?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                var organization = await _datasourcesService.ListOrganizationAsync();

                // If not admin, filter organizations by token's orgId
                if (role != "Admin")
                {
                    // Find organization by token
                    organization.Data = organization.Data!.Where(x => x.Id == int.Parse(orgId ?? "0")).ToList();

                }

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


        [Authorize]
        [HttpPost("EventLog")]
        public async Task<IActionResult> PacomEventLog( string OrganizationCode, DateTime StartDate, DateTime EndDate)
        {
            var username = User.Identity?.Name ?? "User";
            var orgId = User.FindFirst("OrganizationId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // If not admin, filter organizations by token's orgId
            if (role != "Admin")
            {
                // Find organization by token
                var organization = await _datasourcesService.ListOrganizationAsync();
                OrganizationCode = organization.Data!.First(x => x.Id == int.Parse(orgId ?? "0")).Code;
            }

            // Define Malaysia time zone (same as Singapore)
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

            // Convert the received local times to UTC
            DateTime startUtc = TimeZoneInfo.ConvertTimeToUtc(StartDate, malaysiaTimeZone);
            DateTime endUtc = TimeZoneInfo.ConvertTimeToUtc(EndDate, malaysiaTimeZone);

            var result = DatasourcesService.GetEvent(startUtc, endUtc, OrganizationCode);
            return Ok(result);
        }


        [Authorize]
        [HttpPost("Webhhok")]
        public async Task<IActionResult> PacomWebhook( string OrganizationCode, bool Actived, string WebhookUrl)
        {

            var username = User.Identity?.Name ?? "User";
            var orgId = User.FindFirst("OrganizationId")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var Tenant = await _datasourcesService.ListOrganizationAsync();

            // If not admin, filter organizations by token's orgId
            if (role != "Admin")
            {
                // Find organization by token
                var code = Tenant.Data!.First(x => x.Id == int.Parse(orgId ?? "0")).Code;

                if (!Tenant.Data!.Any(x => x.Code == OrganizationCode))
                    return BadRequest(new { Message = "Invalid organization data." });


                if (code != OrganizationCode)
                {
                    return BadRequest(new { Message = "Invalid organization data." });
                }
            }


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
