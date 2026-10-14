using Microsoft.AspNetCore.Mvc;
namespace Messenger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new 
            { status = "Healthy",
              message = "API is running",
            });
        }
    }
}
