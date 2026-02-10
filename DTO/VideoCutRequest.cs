namespace FunctionalitiesWebAPI.DTO
{
    public class VideoCutRequest
    {
        public IFormFile Video { get; set; } = null!;

        // format: seconds OR hh:mm:ss
        public string StartTime { get; set; } = "00:00:00";
        public string EndTime { get; set; } = "00:01:00";
    }
}
