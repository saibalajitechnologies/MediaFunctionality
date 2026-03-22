namespace FunctionalitiesWebAPI.DTO
{
    public class ImageMeta
    {
        public int Duration { get; set; }
        public string Transition { get; set; } = "fade";
        public double TransitionDuration { get; set; } = 1;
    }
}
