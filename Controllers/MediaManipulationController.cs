using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xabe.FFmpeg;

namespace FunctionalitiesWebAPI.Controllers
{
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    [Route("api/[controller]")]
    [ApiController]
    public class MediaManipulationController : ControllerBase
    {
        private readonly ILogger<MediaManipulationController> _logger;

        public MediaManipulationController(ILogger<MediaManipulationController> logger)
        {
            _logger = logger;
        }

        [HttpPost("CompressVideos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompressVideos([FromForm] UploadDto upload)
        {
            var allowedVideoTypes = new[] { "video/mp4", "video/x-m4v", "video/webm", "video/x-msvideo", "video/x-ms-wmv", "video/quicktime"};
            if (FileHelper.ValidateFile(upload?.File, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult error)
                return error;

            var inputPath = FileHelper.GenerateTempPath(".mp4");
            var outputPath = FileHelper.GenerateTempPath("_compressed.mp4");

            try
            {
                await FileHelper.SaveFileAsync(upload.File, inputPath);                
                await MediaManipulationHelper.CompressMediaVideo(inputPath, outputPath);
                
                var fileBytes = await FileHelper.ReadFileAsBytesAsync(outputPath);
                return File(fileBytes, "video/mp4", "compressed.mp4");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video compression failed.");
                return Problem("Video compression failed.", statusCode: 500);
            }
            finally
            {
                FileHelper.SafeDelete(inputPath);
                FileHelper.SafeDelete(outputPath);
            }
        }

        [HttpPost("CompressAudio")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompressAudio([FromForm] UploadDto upload)
        {
            var allowedAudioTypes = new[] { "audio/mpeg", "audio/mp3", "audio/x-m4a", "audio/wav", "audio/aac" };

            if (FileHelper.ValidateFile(upload?.File, 50 * 1024 * 1024, "Audio", allowedAudioTypes) is IActionResult error)
                return error;


            var ext = Path.GetExtension(upload.File.FileName);
            var inputPath = FileHelper.GenerateTempPath(ext);

            try
            {
                await FileHelper.SaveFileAsync(upload.File, inputPath);

                var outputPath = await MediaManipulationHelper.CompressAudio(inputPath);
                var fileBytes = await FileHelper.ReadFileAsBytesAsync(outputPath);
                return File(fileBytes, "audio/mp3", "compressed.mp3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio compression failed.");
                return Problem("Audio compression failed.", statusCode: 500);
            }
            finally
            {
                FileHelper.SafeDelete(inputPath);
            }
        }

        [HttpPost("CompressImages")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompressImages([FromForm] UploadDto upload)
        {
            var allowedImageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };

            if (FileHelper.ValidateFile(upload?.File, 10 * 1024 * 1024, "Image", allowedImageTypes) is IActionResult error)
                return error;

            try
            {
                using var stream = upload.File.OpenReadStream();
                var compressedBytes = await MediaManipulationHelper.CompressImage(stream, quality: 50);
                var ext = Path.GetExtension(upload.File.FileName).ToLower();
                string contentType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };
                return File(compressedBytes, contentType, $"compressed{ext}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image compression failed.");
                return Problem("Image compression failed.", statusCode: 500);
            }
        }


        [HttpPost("MergeVideos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MergeVideos([FromForm] List<IFormFile> filesList)
        {
            var allowedVideoTypes = new[] { "video/mp4", "video/x-m4v", "video/webm", "video/x-msvideo", "video/quicktime" };

            if (FileHelper.ValidateFileCount(filesList, 2, "video files") is IActionResult error)
                return error;

            string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Guid.NewGuid().ToString());
            Directory.CreateDirectory(uploadDir);

            try
            {
                List<string> savedFileNames = new();

                foreach (var file in filesList)
                {
                    if (FileHelper.ValidateFile(file, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult err)
                        return err;

                    var filePath = Path.Combine(uploadDir, file.FileName);
                    await FileHelper.SaveFileAsync(file, filePath);
                    savedFileNames.Add(file.FileName);
                }

                await MediaManipulationHelper.MergeVideosAsync(uploadDir, savedFileNames);

                string mergedFilePath = Path.Combine(uploadDir, "merged.mp4");
                return PhysicalFile(mergedFilePath, "video/mp4", "merged.mp4");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal Server Error", details = ex.Message, stack = ex.StackTrace });
            }
        }


        [HttpPost("MergeAudios")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MergeAudios([FromForm(Name = "filesList")] List<IFormFile> audioFiles)
        {
            var allowedAudioTypes = new[] { "audio/mpeg", "audio/mp3", "audio/x-m4a", "audio/wav", "audio/aac" };

            if (FileHelper.ValidateFileCount(audioFiles, 2, "audio files") is IActionResult error)
                return error;

            string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "TempAudio");
            Directory.CreateDirectory(tempPath);

            var inputPaths = new List<string>();
            foreach (var file in audioFiles)
            {
                if (FileHelper.ValidateFile(file, 50 * 1024 * 1024, "Audio", allowedAudioTypes) is IActionResult err)
                    return err;
                var filePath = Path.Combine(tempPath, file.FileName);
                await FileHelper.SaveFileAsync(file, filePath);
                inputPaths.Add(filePath);
            }

            string outputFile = Path.Combine(tempPath, $"merged_{Guid.NewGuid()}.mp3");

            string ffmpegpath = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
            FFmpeg.SetExecutablesPath(ffmpegpath);

            var conversion = FFmpeg.Conversions.New();
            foreach (var input in inputPaths)
            {
                conversion.AddParameter($"-i \"{input}\"", ParameterPosition.PreInput);
            }

            string filter = $"concat=n={inputPaths.Count}:v=0:a=1[out]";
            conversion.AddParameter($"-filter_complex \"{filter}\" -map \"[out]\" \"{outputFile}\"");

            await conversion.Start();

            var fileBytes = await FileHelper.ReadFileAsBytesAsync(outputFile);
            Directory.Delete(tempPath, true);
            return File(fileBytes, "audio/mpeg", "merged_output.mp3");
        }
    }
}
