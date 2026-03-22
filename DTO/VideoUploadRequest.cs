namespace FunctionalitiesWebAPI.DTO;

#nullable disable
public class VideoUploadRequest
{
    public IFormFile Image { get; set; }
    public IFormFile Audio { get; set; }
}
