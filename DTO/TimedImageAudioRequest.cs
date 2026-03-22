namespace FunctionalitiesWebAPI.DTO;

#nullable disable
public class TimedImageAudioRequest
{
    public List<TimedImageDto> Images { get; set; }
    public IFormFile Audio { get; set; }
}
