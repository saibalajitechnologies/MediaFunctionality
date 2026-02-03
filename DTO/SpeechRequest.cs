namespace FunctionalitiesWebAPI.DTO
{
    public class SpeechRequest
    {
        public string Scenario { get; set; }  // story, meeting, narration, etc.
        public string Content { get; set; }   // SSML or plain text
        public string VoiceName { get; set; } // optional override
        public string OutputFormat { get; set; } = "mp3";
    }
}
