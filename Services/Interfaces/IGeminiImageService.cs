namespace FunctionalitiesWebAPI.Services.Interfaces;

public interface IGeminiImageService
{
    Task<string> GenerateImage(string prompt);
}
