using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PACOM.WebApp
{
    [Route("api/[controller]")]
    [ApiController]
    public class API2Controller : ControllerBase
    {

        [HttpGet("{name}")]
        public IActionResult GetByName(string name)
        {
            return Ok($"Hello, {name}!");
        }
    }
}
