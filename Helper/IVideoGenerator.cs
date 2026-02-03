namespace FunctionalitiesWebAPI.Helper
{
    public interface IVideoGenerator
    {
        Task GenerateTimedImageVideoAsync(List<(string imagePath, int duration)> segments, string audioPath, string outputPath);
    }

}
