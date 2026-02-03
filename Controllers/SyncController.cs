using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Helper;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;

namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly IAudioVideoSyncService _service;

        public SyncController(IAudioVideoSyncService service)
        {
            _service = service;
        }


        [HttpPost("single-audio/multiple-videos")]
        public async Task<IActionResult> SyncMultiple([FromForm] SyncMultipleRequest request)//([FromForm] IFormFile audio, [FromForm] List<IFormFile> videos)
        {
            var result = await _service.SyncSingleAudioMultipleVideos(request.Audio, request.Videos);
            return Ok(result);
        }

        [HttpPost("stretch-audio")]
        public async Task<IActionResult> Stretch([FromForm] StretchRequest request)//([FromForm] IFormFile audio, [FromForm] IFormFile video)
        {
            var result = await _service.StretchAudioToMatchVideo(request.Audio, request.Video);
            return Ok(result);
        }

        [HttpPost("script-based")]
        public async Task<IActionResult> ScriptBased([FromForm] ScriptBasedRequest request)
        {
            try
            {
                if (request.ScriptJson == null || request.ScriptJson.Count == 0)
                    return BadRequest("ScriptJson is required");

                // Merge all form fields into a single JSON array
                string mergedJson = "[" + string.Join(",", request.ScriptJson) + "]";

                // Deserialize final JSON correctly
                var scriptItems = JsonConvert.DeserializeObject<List<ScriptItem>>(mergedJson);

                if (scriptItems == null || scriptItems.Count == 0)
                    return BadRequest("Invalid ScriptJson format");

                var result = await _service.ScriptBasedSync(request.Audio, scriptItems, request.Videos);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
