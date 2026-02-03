using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace FunctionalitiesWebAPI.DTO
{
    public class TimedImageDto2
    {
        [Required]
        public List<IFormFile> Images { get; set; }

        [Required]
        public string Durations { get; set; }

        [Required]
        //[SwaggerSchema("Audio file in MP3 or WAV format")]
        public IFormFile Audio { get; set; }
    }
}
