namespace FunctionalitiesWebAPI.Services;

public class GeminiImageService 
{
    private readonly HttpClient _httpClient;
    private readonly string apiKey = "YOUR_GEMINI_API_KEY";

    public GeminiImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateImage(string prompt)
    {
        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent?key={apiKey}",
            request
        );

        var result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
