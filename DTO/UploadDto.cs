using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.DTO
{
    public class UploadDto
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; }
    }
}
