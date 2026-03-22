using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Services.Interfaces;
using System.Threading.Channels;

namespace FunctionalitiesWebAPI.Services;

public class VideoQueue : IVideoQueue
{
    private readonly Channel<VideoJob> _queue;

    public VideoQueue()
    {
        _queue = Channel.CreateUnbounded<VideoJob>();
    }

    public async ValueTask EnqueueAsync(VideoJob job)
    {
        await _queue.Writer.WriteAsync(job);
    }

    public async ValueTask<VideoJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}