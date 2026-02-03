using Azure.Core;
using FunctionalitiesWebAPI.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech;
using System.Diagnostics;
using System.Net.Http.Headers;




namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpeechController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public SpeechController(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
        }

        [HttpPost("GenerateHFTTS")]
        public async Task<IActionResult> GenerateHFTTS([FromBody] SpeechRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Content cannot be empty.");

            var hfToken = Environment.GetEnvironmentVariable("HF_API_TOKEN");
            if (string.IsNullOrWhiteSpace(hfToken))
                return StatusCode(500, new { error = "Missing HF_API_TOKEN env var" });

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);

            // Choose a TTS model name available on HF inference, e.g., "espnet/kan-bayashi_ljspeech_vits"
            var model = "espnet/kan-bayashi_ljspeech_vits"; // example - pick a model suitable for your language
            var url = $"https://api-inference.huggingface.co/models/{model}";

            var payload = new { inputs = request.Content };
            var response = await client.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var txt = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, txt);
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync();

            var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
            Directory.CreateDirectory(mediaPath);

            var filename = Guid.NewGuid() + ".wav"; // HF often returns wav
            var filePath = Path.Combine(mediaPath, filename);
            await System.IO.File.WriteAllBytesAsync(filePath, audioBytes);

            var urlResponse = $"{Request.Scheme}://{Request.Host}/media/{filename}";
            return Ok(new { message = "HF TTS generated", audioUrl = urlResponse });
        }




        [HttpPost("GenerateOpenAITTS")]
        public async Task<IActionResult> GenerateOpenAITTS([FromBody] SpeechRequest request)
        {
            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                return StatusCode(500, new { error = "Missing OpenAI API key" });


            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);



            var body = new
            {
                model = "gpt-4o-mini-tts",
                input = request.Content,
                voice = "alloy" // or verse, shimmer, soft, etc.
            };

            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/audio/speech", body);
                       

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var path = Path.Combine(_env.WebRootPath ?? "wwwroot", "media", Guid.NewGuid() + ".mp3");
            await System.IO.File.WriteAllBytesAsync(path, bytes);

            var url = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(path)}";
            return Ok(new { message = "AI Voice generated", audioUrl = url });
        }


        [HttpPost("GenerateSpeech")]
        public async Task<IActionResult> GenerateSpeech([FromBody] SpeechRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Content cannot be empty.");

            try
            {
                // -----------------------
                // Configure Speech SDK
                // -----------------------
                var speechConfig = SpeechConfig.FromSubscription("YOUR_AZURE_KEY", "YOUR_REGION");

                // Choose voice based on scenario or override
                string voice = request.VoiceName?.Trim() ?? request.Scenario?.ToLower() switch
                {
                    "story" => "en-US-AriaNeural",
                    "meeting" => "en-US-GuyNeural",
                    "indianfemale" => "en-IN-NeerjaNeural",
                    "indianmale" => "en-IN-PrabhatNeural",
                    _ => "en-US-JennyNeural"
                };
                speechConfig.SpeechSynthesisVoiceName = voice;

                // -----------------------
                // Detect if content is SSML
                // -----------------------
                string ssml;
                if (request.Content.TrimStart().StartsWith("<speak"))
                {
                    ssml = request.Content;
                }
                else
                {
                    // Wrap plain text in SSML with scenario-based prosody
                    string rate = request.Scenario?.ToLower() == "story" ? "slow" : "medium";
                    ssml = $@"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
  <voice name='{voice}'>
    <prosody rate='{rate}'>{request.Content}</prosody>
  </voice>
</speak>";
                }

                // -----------------------
                // Prepare output file
                // -----------------------
                var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
                Directory.CreateDirectory(mediaPath);

                var outputFile = Path.Combine(mediaPath, Guid.NewGuid() + "." + request.OutputFormat);

                // -----------------------
                // Synthesize speech
                // -----------------------
                using var synthesizer = new SpeechSynthesizer(speechConfig, null);
                var result = await synthesizer.SpeakSsmlAsync(ssml);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    await System.IO.File.WriteAllBytesAsync(outputFile, result.AudioData);
                    var audioUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputFile)}";
                    return Ok(new { message = "Speech generated successfully.", audioUrl });
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    return StatusCode(500, new { error = $"Synthesis canceled: {cancellation.Reason}, {cancellation.ErrorDetails}" });
                }
                else
                {
                    return StatusCode(500, new { error = "Unknown speech synthesis error" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}