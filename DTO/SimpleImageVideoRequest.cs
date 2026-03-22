namespace FunctionalitiesWebAPI.DTO;

public class SimpleImageVideoRequest
{
    public IFormFile Image { get; set; } = default!;
    public int Duration { get; set; } // seconds
}
