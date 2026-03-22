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
        Task<string> GenerateImageTransitionWithoutAudio(ImageTransitionOnlyRequest request);
        Task<string> GenerateSingleImageVideo(SimpleImageVideoRequest request);

        Task<string> LoopVideoWithAudio(AudioVideoDto request);

        Task<string> ConvertMpegToMp3(FileUploadDto request);
    }
}
