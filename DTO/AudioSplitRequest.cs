namespace FunctionalitiesWebAPI.DTO;

#nullable disable
public class AudioSplitRequest
{
    public IFormFile Audio { get; set; }
    public List<AudioSegmentDto> Segments { get; set; }
}
