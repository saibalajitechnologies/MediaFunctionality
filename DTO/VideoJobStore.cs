using System.Collections.Concurrent;

namespace FunctionalitiesWebAPI.DTO;

public class VideoJobStore
{
    public ConcurrentDictionary<Guid, VideoJob> Jobs { get; } = new();
}
