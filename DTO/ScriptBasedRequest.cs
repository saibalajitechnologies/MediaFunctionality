using FunctionalitiesWebAPI.Helper;

namespace FunctionalitiesWebAPI.DTO
{
    public class ScriptBasedRequest
    {
        public IFormFile Audio { get; set; }
        public List<string> ScriptJson { get; set; }
        public List<IFormFile> Videos { get; set; }
    }
}
