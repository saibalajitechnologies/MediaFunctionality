using FunctionalitiesWebAPI.DTO;

namespace FunctionalitiesWebAPI.Services.Interfaces;

public interface IVideoQueue
{
    ValueTask EnqueueAsync(VideoJob job);
    ValueTask<VideoJob> DequeueAsync(CancellationToken cancellationToken);
}
