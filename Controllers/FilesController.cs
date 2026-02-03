using FunctionalitiesWebAPI.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        [HttpPost("splitmyFile")]
        public async Task<IActionResult> splitMyFile()
        {
            //await FileHelper.readingfiles();
            await EmailSplitter.readingfiles();
            return StatusCode(200, "Success");
        }
    }
}
