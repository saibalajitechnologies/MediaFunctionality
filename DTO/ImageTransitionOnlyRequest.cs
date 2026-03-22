namespace FunctionalitiesWebAPI.DTO
{
    public class ImageTransitionOnlyRequest
    {
        public List<IFormFile> Images { get; set; } = new();
        public List<ImageMeta> Meta { get; set; } = new();
    }
}
