namespace FunctionalitiesWebAPI.DTO;

public class ImageWithDurationDto
{
    public IFormFile Image { get; set; } = default!;
    public int Duration { get; set; }
}
