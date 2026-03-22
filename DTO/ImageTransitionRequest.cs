namespace FunctionalitiesWebAPI.DTO
{
    public class ImageTransitionRequest
    {
        public IFormFile Audio { get; set; } = default!;

        public List<IFormFile> Images { get; set; } = new();
        // JSON string for durations+transitions
        public List<ImageMetaDto> Meta { get; set; } = new();

        //public List<int> Durations { get; set; } = new();

        //public List<string> Transitions { get; set; } = new();

        //public List<double> TransitionDurations { get; set; } = new();
    }
}
