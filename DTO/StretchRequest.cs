namespace FunctionalitiesWebAPI.DTO
{
    public class StretchRequest
    {
        public IFormFile Audio { get; set; } = default!;
        public IFormFile Video { get; set; } = default!;
    }
}
