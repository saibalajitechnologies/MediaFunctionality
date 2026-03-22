namespace FunctionalitiesWebAPI.DTO;

#nullable disable
public class SyncMultipleRequest
{
    public IFormFile Audio { get; set; }
    public List<IFormFile> Videos { get; set; }
}
