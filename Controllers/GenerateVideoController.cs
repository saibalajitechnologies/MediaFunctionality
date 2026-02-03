using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenerateVideoController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GenerateVideoController> _logger;
        private readonly IVideoGenerator _videoGenerator;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedAudioExtensions = { ".mp3", ".wav" };


        public GenerateVideoController(IWebHostEnvironment env, ILogger<GenerateVideoController> logger, IVideoGenerator videoGenerator)
        {
            _env = env;
            _logger = logger;
            _videoGenerator = videoGenerator;
        }

        //[HttpPost("generate-from-timed-images")]
        //public async Task<IActionResult> GenerateFromTimedImages([FromBody] TimedImageAudioRequest2 request)
        //{
        //    if (request.Images == null || !request.Images.Any() || string.IsNullOrWhiteSpace(request.AudioBase64))
        //        return BadRequest("Audio and at least one image with duration are required.");

        //    var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
        //    Directory.CreateDirectory(mediaPath);

        //    var tempFiles = new List<string>();

        //    try
        //    {
        //        // Save audio
        //        var audioBytes = GetBytesFromBase64(request.AudioBase64, out string audioExt);
        //        var audioPath = Path.Combine(mediaPath, $"{Guid.NewGuid()}{audioExt}");
        //        await System.IO.File.WriteAllBytesAsync(audioPath, audioBytes);
        //        tempFiles.Add(audioPath);

        //        var segmentList = new List<(string imagePath, int duration)>();

        //        foreach (var img in request.Images)
        //        {
        //            var imageBytes = GetBytesFromBase64(img.ImageBase64, out string imgExt);
        //            var imgPath = Path.Combine(mediaPath, $"{Guid.NewGuid()}{imgExt}");
        //            await System.IO.File.WriteAllBytesAsync(imgPath, imageBytes);
        //            tempFiles.Add(imgPath);

        //            segmentList.Add((imgPath, img.Duration));
        //        }

        //        // Generate video
        //        var outputVideoPath = Path.Combine(mediaPath, $"{Guid.NewGuid()}.mp4");
        //        await _videoGenerator.GenerateTimedImageVideoAsync(segmentList, audioPath, outputVideoPath);

        //        var videoUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputVideoPath)}";
        //        return Ok(new { message = "Video created successfully.", videoUrl });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error generating video");
        //        return StatusCode(500, new { error = "Internal server error while generating video." });
        //    }
        //    finally
        //    {
        //        foreach (var file in tempFiles)
        //        {
        //            try { System.IO.File.Delete(file); } catch { /* ignore */ }
        //        }
        //    }
        //}


        [HttpPost("GenerateFromTimedImages")]
        public async Task<IActionResult> GenerateFromTimedImages([FromForm] TimedImageAudioRequest2 form)
        {
            if (form.Images == null || !form.Images.Any() || form.Audio == null || string.IsNullOrEmpty(form.Durations))
                return BadRequest("Missing files or durations.");

            var durations = System.Text.Json.JsonSerializer.Deserialize<List<int>>(form.Durations);

            if (durations == null || durations.Count != form.Images.Count)
                return BadRequest("Durations count must match images count.");

        
            var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
            Directory.CreateDirectory(mediaPath);

            var tempFiles = new List<string>();
            var segmentList = new List<(string imagePath, int duration)>();

            try
            {
                // Save audio
                var audioExt = Path.GetExtension(form.Audio.FileName);
                var audioPath = Path.Combine(mediaPath, $"{Guid.NewGuid()}{audioExt}");
                using (var audioStream = new FileStream(audioPath, FileMode.Create))
                {
                    await form.Audio.CopyToAsync(audioStream);
                }

                // Save images with durations
                for (int i = 0; i < form.Images.Count; i++)
                {
                    var img = form.Images[i];
                    var duration = durations[i];

                    var imgExt = Path.GetExtension(img.FileName);
                    var imgPath = Path.Combine(mediaPath, $"{Guid.NewGuid()}{imgExt}");
                    using (var imgStream = new FileStream(imgPath, FileMode.Create))
                    {
                        await img.CopyToAsync(imgStream);
                    }
                    tempFiles.Add(imgPath);

                    segmentList.Add((imgPath, duration));
                }

                // Generate video
                var outputVideoPath = Path.Combine(mediaPath, $"{Guid.NewGuid()}.mp4");
                await _videoGenerator.GenerateTimedImageVideoAsync(segmentList, audioPath, outputVideoPath);

                var videoUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputVideoPath)}";
                return Ok(new { message = "Video created successfully.", videoUrl });
            }
            finally
            {
                // Cleanup temp files
                foreach (var file in tempFiles)
                {
                    try { System.IO.File.Delete(file); } catch { }
                }
            }
        }



        private byte[] GetBytesFromBase64(string base64WithPrefix, out string fileExt)
        {
            var dataParts = base64WithPrefix.Split(',');
            if (dataParts.Length != 2)
                throw new Exception("Invalid base64 format.");

            var header = dataParts[0]; // e.g., "data:image/jpeg;base64"
            var base64 = dataParts[1];

            if (header.Contains("image/png"))
                fileExt = ".png";
            else if (header.Contains("image/jpeg") || header.Contains("image/jpg"))
                fileExt = ".jpg";
            else if (header.Contains("audio/mp3"))
                fileExt = ".mp3";
            else if (header.Contains("audio/mpeg"))
                fileExt = ".mp3";
            else if (header.Contains("audio/wav"))
                fileExt = ".wav";
            else
                throw new Exception("Unsupported media type.");

            return Convert.FromBase64String(base64);
        }

    }
}