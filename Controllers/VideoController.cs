using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [RequestSizeLimit(500_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
    public class VideoController : ControllerBase
    {
        private readonly IVideoService _videoService;

        public VideoController(IVideoService videoService)
        {
            _videoService = videoService;
        }

        private string BuildMediaUrl(string fileName)
        {
            return $"{Request.Scheme}://{Request.Host}/media/{fileName}";
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateVideo([FromForm] VideoUploadRequest request)
        {
            var fileName = await _videoService.GenerateVideoFromImageAndAudio(request);
            return Ok(new { message = "Video generated successfully.", videoUrl = BuildMediaUrl(fileName) });
        }

        [HttpPost("GenerateVideowithAudio")]
        public async Task<IActionResult> GenerateVideowithAudio([FromForm] AudioVideoDto request)
        {
            var fileName = await _videoService.ReplaceVideoAudio(request);
            return Ok(new { message = "Video generated successfully.", videoUrl = BuildMediaUrl(fileName) });
        }

        [HttpPost("MergeAudioWithVideo")]
        public async Task<IActionResult> MergeAudioWithVideo([FromForm] AudioVideoDto request)
        {
            var fileName = await _videoService.MergeAudioWithVideo(request);
            return Ok(new { message = "Video merged successfully.", videoUrl = BuildMediaUrl(fileName) });
        }

        [HttpPost("cut-video")]
        public async Task<IActionResult> CutVideo([FromForm] VideoCutRequest request)
        {
            var fileName = await _videoService.CutVideo(request);
            return Ok(new { message = "Video cut successfully", videoUrl = BuildMediaUrl(fileName) });
        }

        [HttpPost("extract-audio")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExtractAudio([FromForm] VideoUploadDto request)
        {
            var fileName = await _videoService.ExtractAudio(request);
            return Ok(new { message = "Audio extracted successfully", audioUrl = BuildMediaUrl(fileName) });
        }

        [HttpPost("generate-timed-images")]
        public async Task<IActionResult> GenerateTimedImagesVideo([FromForm] TimedImageAudioRequest request)
        {
            var fileName = await _videoService.GenerateFromTimedImages(request);
            return Ok(new { message = "Video generated successfully.", videoUrl = BuildMediaUrl(fileName) });
        }

        [HttpPost("transition-videos")]
        public async Task<IActionResult> TransitionVideo([FromForm] ImageTransitionRequest request)
        {
            var fileName = await _videoService.GenerateImageTranstion(request);
            return Ok(request);
            //return Ok(new { message = "Video generated successfully.", videoUrl = BuildMediaUrl(fileName) });
        }


        [HttpPost("split-audio")]
        public async Task<IActionResult> SplitAudio([FromForm] AudioSplitRequest request)
        {
            var fileName = await _videoService.SplitAndKeepAudio(request);
            return Ok(new { message = "Audio split successfully.", audioUrl = BuildMediaUrl(fileName) });
        }

        [HttpPost("transition-no-audio")]
        public async Task<IActionResult> TransitionVideoNoAudio([FromForm] ImageTransitionOnlyRequest request)
        {
            Console.WriteLine($"Images: {request.Images.Count}");
            Console.WriteLine($"Meta: {request.Meta.Count}");

            var fileName = await _videoService.GenerateImageTransitionWithoutAudio(request);
            return Ok(new { message = "Video generated successfully.", videoUrl = BuildMediaUrl(fileName) });
        }


        [HttpPost("GenerateVideoForimage")]
        public async Task<IActionResult> GenerateVideoForimage([FromForm] SimpleImageVideoRequest request)
        {
            var fileName = await _videoService.GenerateSingleImageVideo(request);

            return Ok(new
            {
                message = "Video generated successfully.",
                videoUrl = BuildMediaUrl(fileName)
            });
        }

        [HttpPost("loop-video-with-audio")]
        public async Task<IActionResult> LoopVideoWithAudio([FromForm] AudioVideoDto request)
        {
            var fileName = await _videoService.LoopVideoWithAudio(request);

            return Ok(new
            {
                message = "Video looped until audio ends successfully.",
                videoUrl = BuildMediaUrl(fileName)
            });
        }

        [HttpPost("convert-mpeg-to-mp3")]
        public async Task<IActionResult> ConvertMpegToMp3([FromForm] FileUploadDto request)
        {
            var fileName = await _videoService.ConvertMpegToMp3(request);

            return Ok(new
            {
                message = "Converted successfully",
                fileUrl = BuildMediaUrl(fileName)
            });
        }
    }
}