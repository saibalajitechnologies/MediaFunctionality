namespace FunctionalitiesWebAPI.DTO
{
    public class ImageTransitionDto
    {
        public IFormFile Image { get; set; } = default!;

        public int Duration { get; set; } // seconds

        public string Transition { get; set; } = "fade"; // default

        public double TransitionDuration { get; set; } = 1; // seconds
    }
}
