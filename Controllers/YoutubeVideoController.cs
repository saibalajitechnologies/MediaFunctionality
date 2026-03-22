using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class YoutubeVideoController : ControllerBase
{
    private readonly IVideoQueue _queue;
    private readonly VideoJobStore _store;

    public YoutubeVideoController(IVideoQueue queue, VideoJobStore store)
    {
        _queue = queue;
        _store = store;
    }

    [HttpGet("status/{id}")]
    public IActionResult GetStatus(Guid id)
    {
        if (!_store.Jobs.TryGetValue(id, out var job))
            return NotFound();

        return Ok(new
        {
            job.Status,
            job.ProcessedPath,
            job.ErrorMessage
        });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, int episodeNumber)
    {
        var videosFolder = Path.Combine("wwwroot", "media");
        Directory.CreateDirectory(videosFolder); // Ensure folder exists

        var filePath = Path.Combine(videosFolder, $"{Guid.NewGuid()}.mp4");

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var job = new VideoJob
        {
            OriginalPath = filePath,
            EpisodeNumber = episodeNumber
        };

        // ✅ Add to in-memory store
        _store.Jobs[job.Id] = job;

        // ✅ Enqueue job
        await _queue.EnqueueAsync(job);

        return Ok(new
        {
            job.Id,
            job.Status
        });
    }
}
