using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Helper;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        [HttpPost("ExtractUrl")]
        public IActionResult ExtractUrl([FromBody] string request)
        {
            var urls = EmailSplitter.ExtractURLPatterns(request);
            return Ok(urls);
        }

        [HttpPost("splitmyFile")]
        public async Task<IActionResult> SplitMyFile()
        {
            //await FileHelper.readingfiles();
            await EmailSplitter.readingfiles();
            return StatusCode(200, "Success");
        }
    }
}
