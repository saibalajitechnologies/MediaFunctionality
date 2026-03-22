using FunctionalitiesWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AIController : ControllerBase
{
    private readonly GeminiImageService _service;

    public AIController(GeminiImageService service)
    {
        _service = service;
    }

    [HttpPost("generate-image")]
    public async Task<IActionResult> GenerateImage([FromBody] string prompt)
    {
        var result = await _service.GenerateImage(prompt);
        return Ok(result);
    }
}
