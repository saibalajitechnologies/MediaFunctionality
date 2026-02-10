namespace FunctionalitiesWebAPI.DTO
{
    public class TimedImageDto
    {
        public IFormFile Image { get; set; } = default!;
        public int Duration { get; set; } // Duration in seconds
    }
}
