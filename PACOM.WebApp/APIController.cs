using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PACOM.Services;
using PacomLibrary;
using PacomLibrary.Models;

namespace PACOM.WebApp
{
    [Route("[controller]")]
    [ApiController]
    public class APIController : ControllerBase
    {

        [HttpPost("EventLog")]
        public IActionResult EventLog( string OrganizationCode)
        {
            var result = PacomIntegration.GetEvent(OrganizationCode);
            return Ok(result);
        }

        [HttpGet("List User")]
        public IActionResult ListUser( string OrganizationCode)
        {
            var result = PacomIntegration.ListAllUsers(OrganizationCode);
            return Ok(result);
        }
    }
}
