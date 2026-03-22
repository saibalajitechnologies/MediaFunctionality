namespace FunctionalitiesWebAPI.DTO;

public class VideoJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? OriginalPath { get; set; }
    public int EpisodeNumber { get; set; }

    public string Status { get; set; } = "Pending";
    public string? ProcessedPath { get; set; }
    public string? ErrorMessage { get; set; }
}
