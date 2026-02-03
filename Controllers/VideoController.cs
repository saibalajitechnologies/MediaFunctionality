using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public VideoController(IWebHostEnvironment env)
        {
            _env = env;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateVideo([FromForm] VideoUploadRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            var image = request.Image;
            var audio = request.Audio;

            if (image == null || audio == null)
                return BadRequest("Both image and audio are required.");

            try
            {
                var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
                Directory.CreateDirectory(mediaPath);

                var imagePath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(image.FileName));
                var audioPath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(audio.FileName));
                var outputVideoPath = Path.Combine(mediaPath, Guid.NewGuid() + ".mp4");

                // Save uploaded files
                using (var imgStream = new FileStream(imagePath, FileMode.Create))
                    await image.CopyToAsync(imgStream);

                using (var audioStream = new FileStream(audioPath, FileMode.Create))
                    await audio.CopyToAsync(audioStream);

                Console.WriteLine($"[⏱️] Upload + Save Time: {stopwatch.Elapsed.TotalSeconds}s");

                // Generate video
                stopwatch.Restart();
                var videoGen = new VideoGenerator();
                videoGen.GenerateVideo(imagePath, audioPath, outputVideoPath);
                Console.WriteLine($"[⏱️] FFmpeg Execution Time: {stopwatch.Elapsed.TotalSeconds}s");

                var videoUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputVideoPath)}";
                return Ok(new { message = "Video generated successfully.", videoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }

        }


        [HttpPost("GenerateVideowithAudio")]
        public async Task<IActionResult> GenerateVideowithAudio([FromForm] AudioVideoDto request)
        {
            var stopwatch = Stopwatch.StartNew();

            var audio = request.Audio;
            var video = request.Video;

            if (audio == null || video == null)
                return BadRequest("Both audio and video are required.");

            try
            {
                var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
                Directory.CreateDirectory(mediaPath);

                var audioPath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(audio.FileName));
                var videoPath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(video.FileName));
                var outputVideoPath = Path.Combine(mediaPath, Guid.NewGuid() + ".mp4");

                // Save uploaded files
                using (var audioStream = new FileStream(audioPath, FileMode.Create))
                    await audio.CopyToAsync(audioStream);

                using (var videoStream = new FileStream(videoPath, FileMode.Create))
                    await video.CopyToAsync(videoStream);

                Console.WriteLine($"[⏱️] Upload + Save Time: {stopwatch.Elapsed.TotalSeconds}s");

                // Generate video
                stopwatch.Restart();
                var videoGen = new VideoGenerator();
                videoGen.GenerateVideoFromAudio(videoPath, audioPath, outputVideoPath);
                Console.WriteLine($"[⏱️] FFmpeg Execution Time: {stopwatch.Elapsed.TotalSeconds}s");

                var videoUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputVideoPath)}";
                return Ok(new { message = "Video generated successfully.", videoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }

        }

        [HttpPost("MergeAudioWithVideo")]
        public async Task<IActionResult> MergeAudioWithVideo([FromForm] AudioVideoDto request)
        {
            var stopwatch = Stopwatch.StartNew();

            var audio = request.Audio;
            var video = request.Video;

            if (audio == null || video == null)
                return BadRequest("Both audio and video files are required.");

            try
            {
                var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
                Directory.CreateDirectory(mediaPath);

                var audioPath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(audio.FileName));
                var videoPath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(video.FileName));
                var outputVideoPath = Path.Combine(mediaPath, Guid.NewGuid() + ".mp4");

                // Save uploaded files
                using (var audioStream = new FileStream(audioPath, FileMode.Create))
                    await audio.CopyToAsync(audioStream);

                using (var videoStream = new FileStream(videoPath, FileMode.Create))
                    await video.CopyToAsync(videoStream);

                Console.WriteLine($"[⏱️] Upload Time: {stopwatch.Elapsed.TotalSeconds}s");
                stopwatch.Restart();

                // Merge audio and video (no looping)
                var videoGen = new VideoGenerator();
                videoGen.MergeAudioWithVideo(videoPath, audioPath, outputVideoPath);

                Console.WriteLine($"[⏱️] FFmpeg Execution Time: {stopwatch.Elapsed.TotalSeconds}s");

                var videoUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputVideoPath)}";
                return Ok(new { message = "Video merged successfully.", videoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost("generate-from-timed-images")]
        public async Task<IActionResult> GenerateFromTimedImages([FromForm] TimedImageAudioRequest request)
        {
            if (request.Images == null || !request.Images.Any() || request.Audio == null)
                return BadRequest("Audio and at least one image with duration are required.");

            try
            {
                var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
                Directory.CreateDirectory(mediaPath);

                var audioPath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(request.Audio.FileName));
                using (var audioStream = new FileStream(audioPath, FileMode.Create))
                    await request.Audio.CopyToAsync(audioStream);

                var segmentList = new List<(string imagePath, int duration)>();

                foreach (var imageDto in request.Images)
                {
                    var imagePath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(imageDto.Image.FileName));
                    using (var imgStream = new FileStream(imagePath, FileMode.Create))
                        await imageDto.Image.CopyToAsync(imgStream);

                    segmentList.Add((imagePath, imageDto.Duration));
                }

                var outputVideoPath = Path.Combine(mediaPath, Guid.NewGuid() + ".mp4");

                var videoGen = new VideoGenerator();
                videoGen.GenerateTimedImageVideo(segmentList, audioPath, outputVideoPath);

                var videoUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputVideoPath)}";
                return Ok(new { message = "Timed image video generated successfully.", videoUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost("SplitAndKeepAudio")]
        public async Task<IActionResult> SplitAndKeepAudio([FromForm] AudioSplitRequest request)
        {
            if (request.Audio == null || request.Segments == null || !request.Segments.Any())
                return BadRequest("Audio file and at least one segment are required.");

            try
            {
                var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
                Directory.CreateDirectory(mediaPath);

                var audioPath = Path.Combine(mediaPath, Guid.NewGuid() + Path.GetExtension(request.Audio.FileName));
                using (var audioStream = new FileStream(audioPath, FileMode.Create))
                    await request.Audio.CopyToAsync(audioStream);

                var outputPath = Path.Combine(mediaPath, Guid.NewGuid() + ".mp3");

                var generator = new VideoGenerator();
                generator.SplitAndKeepAudio(audioPath, request.Segments, outputPath);

                var audioUrl = $"{Request.Scheme}://{Request.Host}/media/{Path.GetFileName(outputPath)}";
                return Ok(new { message = "Audio split successfully.", audioUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


    }
}