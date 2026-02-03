namespace FunctionalitiesWebAPI.DTO
{
    public class AudioSplitRequest
    {
        public IFormFile Audio { get; set; }
        public List<AudioSegmentDto> Segments { get; set; }
    }
}
