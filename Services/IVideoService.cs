using FunctionalitiesWebAPI.DTO;

namespace FunctionalitiesWebAPI.Services
{
    public interface IVideoService
    {
        Task<string> GenerateVideoFromImageAndAudio(VideoUploadRequest request);

        Task<string> ReplaceVideoAudio(AudioVideoDto request);

        Task<string> MergeAudioWithVideo(AudioVideoDto request);

        Task<string> CutVideo(VideoCutRequest request);

        Task<string> ExtractAudio(VideoUploadDto request);
        Task<string> GenerateFromTimedImages(TimedImageAudioRequest request);
        Task<string> SplitAndKeepAudio(AudioSplitRequest request);
        Task<string> GenerateImageTranstion(ImageTransitionRequest request);
    }
}
