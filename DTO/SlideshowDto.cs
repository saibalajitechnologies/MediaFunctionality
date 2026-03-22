namespace FunctionalitiesWebAPI.DTO;

public class SlideshowDto
{
    public List<IFormFile> Images { get; set; } = new();
    public IFormFile? Audio { get; set; }
}
