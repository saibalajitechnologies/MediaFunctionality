namespace FunctionalitiesWebAPI.DTO
{
    public class SsmlRequest
    {
        public string Scenario { get; set; } // e.g. "story", "meeting", "narration"
        public string SsmlContent { get; set; } // full SSML text
        public string VoiceName { get; set; } // optional specific voice
        public string OutputFormat { get; set; } = "mp3"; // default
    }
}
