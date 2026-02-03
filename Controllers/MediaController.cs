//using FunctionalitiesWebAPI.DTO;
//using FunctionalitiesWebAPI.Helper;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace FunctionalitiesWebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class MediaController : ControllerBase
//    {
//        [HttpPost("CompressVideos")]
//        [Consumes("multipart/form-data")]
//        public async Task<IActionResult> CompressVideos([FromForm] UploadDto upload)
//        {
//            if (upload?.File == null || upload.File.Length == 0)
//                return BadRequest("Invalid file.");

//            // Unique temp file paths for each request
//            var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_input.mp4");
//            var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_compressed.mp4");

//            try
//            {
//                // Save uploaded file to disk
//                using (var stream = new FileStream(inputPath, FileMode.Create, FileAccess.Write, FileShare.None))
//                {
//                    await upload.File.CopyToAsync(stream);
//                }

//                // Run FFmpeg compression
//                await MediaCompress.CompressMediaVideo(inputPath, outputPath);

//                // Read compressed video
//                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
//                return File(fileBytes, "video/mp4", "compressed.mp4");
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Video compression failed: {ex.Message}");
//            }
//            finally
//            {
//                // Ensure files are deleted safely even if locked temporarily
//                SafeDelete(inputPath);
//                SafeDelete(outputPath);
//            }
//        }

//        /// <summary>
//        /// Tries to delete the file safely with retry logic.
//        /// </summary>
//        private void SafeDelete(string path, int retries = 5, int delayMs = 200)
//        {
//            for (int i = 0; i < retries; i++)
//            {
//                try
//                {
//                    if (System.IO.File.Exists(path))
//                    {
//                        System.IO.File.Delete(path);
//                    }
//                    return;
//                }
//                catch (IOException)
//                {
//                    Thread.Sleep(delayMs); // Wait and retry
//                }
//                catch (UnauthorizedAccessException)
//                {
//                    Thread.Sleep(delayMs);
//                }
//            }
//        }


        



//        [HttpPost("CompressAudio")]
//        public async Task<IActionResult> CompressAudio([FromForm] UploadDto upload)
//        {
//            if (upload?.File == null || upload.File.Length == 0)
//                return StatusCode(400, "Invalid File");

//            var ext = Path.GetExtension(upload.File.FileName);
//            var inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_input{ext}");

//            try
//            {
//                // Save uploaded file to disk
//                using (var stream = new FileStream(inputPath, FileMode.Create))
//                {
//                    await upload.File.CopyToAsync(stream);
//                }

//                // Compress audio using FFmpeg
//                var outputPath = await MediaCompress.CompressAudio(inputPath);

//                // Read compressed file as bytes
//                var compressedBytes = await System.IO.File.ReadAllBytesAsync(outputPath);

//                // Determine content-type (based on output, not input!)
//                string contentType = "audio/mp3"; // Because we hardcoded MP3 output

//                return File(compressedBytes, contentType, "compressed.mp3");
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Audio compression failed: {ex.Message}");
//            }
//            finally
//            {
//                // Clean up temp files
//                if (System.IO.File.Exists(inputPath))
//                    System.IO.File.Delete(inputPath);
//            }
//        }

//        [HttpPost("CompressImages")]
//        public async Task<IActionResult> CompressImages([FromForm] UploadDto upload)
//        {
//            if (upload?.File == null || upload.File.Length == 0)
//                return StatusCode(400, "Invalid File");

//            try
//            {
//                using var stream = upload.File.OpenReadStream();

//                // Compress using SkiaSharp
//                var compressedBytes = await MediaCompress.CompressImage(stream, quality: 50);

//                var ext = Path.GetExtension(upload.File.FileName);
//                string contentType = ext.ToLower() switch
//                {
//                    ".jpg" or ".jpeg" => "image/jpeg",
//                    ".png" => "image/png",
//                    ".webp" => "image/webp",
//                    _ => "application/octet-stream"
//                };

//                return File(compressedBytes, contentType, $"compressed{ext}");
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Image compression failed: {ex.Message}");
//            }
//        }


//    }
//}
