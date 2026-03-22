using FunctionalitiesWebAPI.DTO;

namespace FunctionalitiesWebAPI.Helper
{
    public interface IVideoGenerator
    {
        Task GenerateVideoAsync(string imagePath, string audioPath, string outputVideoPath);

        Task GenerateVideoFromAudioAsync(string videoPath, string audioPath, string outputVideoPath);

        Task MergeAudioWithVideoAsync(string videoPath, string audioPath, string outputVideoPath);

        Task ExtractAudioFromVideoAsync(string videoPath, string outputAudioPath);

        Task CutVideoAsync(string inputPath, string startTime, string endTime, string outputPath);

        Task CutVideoAsyncaccurate(string inputPath, string startTime, string endTime, string outputPath);

        Task GenerateTimedImageVideoAsync(List<(string imagePath, int duration)> segments, string audioPath, string outputVideoPath);

        Task SplitAndKeepAudioAsync(string audioPath, List<AudioSegmentDto> segments, string outputPath);

        Task GenerateTimedImageVideoWithTransitionsAsync(List<(string imagePath, int duration, string transition, double transitionDuration)> segments,
        string audioPath, string outputVideoPath);

        Task GenerateVideoFromImageWithTransitions(List<(string imagePath, int duration, string transition, double transitionDuration)> segments, string outputVideoPath);

        Task GenerateSingleImageVideoAsync(string imagePath, int duration, string outputPath);

        Task LoopVideoUntilAudioEndsAsync(string videoPath, string audioPath, string outputPath);

        Task ConvertMpegToMp3Async(string inputPath, string outputPath);
    }
}
