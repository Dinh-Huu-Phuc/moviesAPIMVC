using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Movie_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public StreamController(IWebHostEnvironment env) => _env = env;

        [HttpGet("{file}")]
        public IActionResult Get(string file)
        {
            var path = Path.Combine(_env.ContentRootPath, "uploads", file);
            if (!System.IO.File.Exists(path)) return NotFound();
            var stream = System.IO.File.OpenRead(path);
            return File(stream, "video/mp4", enableRangeProcessing: true);
        }
    }
}
