using FunctionalitiesWebAPI.Helper;

namespace FunctionalitiesWebAPI.Services.Interfaces
{
    public interface IAudioVideoSyncService
    {
        Task<List<string>> SyncSingleAudioMultipleVideos(IFormFile audio, List<IFormFile> videos);
        Task<string> StretchAudioToMatchVideo(IFormFile audio, IFormFile video);
        Task<List<string>> ScriptBasedSync(IFormFile audio, List<ScriptItem> scriptJson, List<IFormFile> videos);
    }
}
