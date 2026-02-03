using System.ComponentModel.DataAnnotations;

namespace FunctionalitiesWebAPI.DTO
{
    public class TimedImageAudioRequest2
    {
        [Required]
        public List<IFormFile> Images { get; set; }

        [Required]
        public string Durations { get; set; }

        [Required]
        public IFormFile Audio { get; set; }
    }
}
