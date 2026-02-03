namespace FunctionalitiesWebAPI.DTO
{
    public class SyncMultipleRequest
    {
        public IFormFile Audio { get; set; }
        public List<IFormFile> Videos { get; set; }
    }
}
