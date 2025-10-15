using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Movie_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public DebugController(IWebHostEnvironment env) { _env = env; }

        [HttpGet("uploads")]
        public IActionResult ListUploads()
        {
            var dir = Path.Combine(_env.ContentRootPath, "uploads");
            var exists = Directory.Exists(dir);
            var files = exists ? Directory.GetFiles(dir).Select(Path.GetFileName).ToArray() : Array.Empty<string>();
            return Ok(new { dir, exists, files });
        }
    }
}
