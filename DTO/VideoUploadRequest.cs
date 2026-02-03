namespace FunctionalitiesWebAPI.DTO
{
    public class VideoUploadRequest
    {
        public IFormFile Image { get; set; }
        public IFormFile Audio { get; set; }
    }
}
