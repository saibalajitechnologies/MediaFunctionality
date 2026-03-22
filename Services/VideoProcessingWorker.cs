using FunctionalitiesWebAPI.Helper;
using FunctionalitiesWebAPI.Services.Interfaces;

namespace FunctionalitiesWebAPI.Services;

public class VideoProcessingWorker : BackgroundService
{
    private readonly IVideoQueue _queue;
    private readonly VideoProcessingService _videoService;

    public VideoProcessingWorker(
        IVideoQueue queue,
        VideoProcessingService videoService)
    {
        _queue = queue;
        _videoService = videoService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await _queue.DequeueAsync(stoppingToken);

            try
            {
                if (string.IsNullOrEmpty(job.OriginalPath))
                {
                    job.Status = "Failed";
                    job.ErrorMessage = "OriginalPath is null or empty.";
                    continue;
                }

                job.Status = "Processing";

                var output = await _videoService.CreateFullYoutubeVideo(
                    job.OriginalPath,
                    job.EpisodeNumber);

                job.ProcessedPath = output;
                job.Status = "Completed";
            }
            catch (Exception ex)
            {
                job.Status = "Failed";
                job.ErrorMessage = ex.Message;
            }
        }
    }
}