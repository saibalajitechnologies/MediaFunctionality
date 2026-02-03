//using FunctionalitiesWebAPI.Helper;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Xabe.FFmpeg;

//namespace FunctionalitiesWebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class MergeController : ControllerBase
//    {
//        public MergeController() { }

//        [HttpPost("MergeVideos")]
//        [Produces("video/mp4")]
//        [Consumes("multipart/form-data")]
//        public async Task<IActionResult> MergeVideos([FromForm] List<IFormFile> filesList)
//        {
//            if (filesList == null || !filesList.Any())
//                return BadRequest("Please upload at least one video file.");

//            string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Guid.NewGuid().ToString());
//            Directory.CreateDirectory(uploadDir);

//            try
//            {
//                List<string> savedFileNames = new();

//                foreach (var file in filesList)
//                {
//                    var filePath = Path.Combine(uploadDir, file.FileName);
//                    using (var stream = new FileStream(filePath, FileMode.Create))
//                    {
//                        await file.CopyToAsync(stream);
//                    }
//                    savedFileNames.Add(file.FileName);
//                }

//                await MergeHelper.MergeVideosAsync(uploadDir, savedFileNames);

//                string mergedFilePath = Path.Combine(uploadDir, "merged.mp4");

//                //return PhysicalFile(mergedFilePath, "video/mp4", "merged_output.mp4");
//                return PhysicalFile(mergedFilePath, "video/mp4"); // ✅ Preview instead of download

//            }
//            catch (Exception ex)
//            {
//                //Send full error in development
//                return StatusCode(500, new
//                {
//                    error = "Internal Server Error",
//                    details = ex.Message,
//                    stack = ex.StackTrace
//                });
//            }
//        }

//        [HttpPost("MergeVideosChecking")]
//        [Produces("video/mp4")]
//        [Consumes("multipart/form-data")]
//        public async Task<IActionResult> MergeVideosChecking([FromForm] List<IFormFile> filesList)
//        {
//            if (filesList == null || !filesList.Any())
//                return BadRequest("Please upload at least one video file.");

//            string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", Guid.NewGuid().ToString());
//            Directory.CreateDirectory(uploadDir);

//            try
//            {
//                List<string> savedFileNames = new();

//                foreach (var file in filesList)
//                {
//                    var filePath = Path.Combine(uploadDir, file.FileName);
//                    using (var stream = new FileStream(filePath, FileMode.Create))
//                    {
//                        await file.CopyToAsync(stream);
//                    }
//                    savedFileNames.Add(file.FileName);
//                }

//                await MergeHelper.MergeVideosAsync(uploadDir, savedFileNames);

//                string mergedFilePath = Path.Combine(uploadDir, "merged.mp4");

//                var result = PhysicalFile(mergedFilePath, "video/mp4"); // ❌ No filename → no auto-download

//                // ✅ Asynchronous cleanup
//                _ = Task.Run(() =>
//                {
//                    try { Directory.Delete(uploadDir, true); } catch { /* log if needed */ }
//                });

//                return result;
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    error = "Internal Server Error",
//                    details = ex.Message,
//                    stack = ex.StackTrace
//                });
//            }
//        }

//        [HttpPost("MergeAudiosChecking")]
//        [Produces("audio/mpeg")]
//        [Consumes("multipart/form-data")]
//        public async Task<IActionResult> MergeAudiosChecking([FromForm(Name = "filesList")] List<IFormFile> audioFiles)
//        {
//            if (audioFiles == null || audioFiles.Count < 2)
//                return BadRequest("At least two audio files are required to merge.");

//            string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "TempAudio");
//            Directory.CreateDirectory(tempPath);

//            var inputPaths = new List<string>();
//            foreach (var file in audioFiles)
//            {
//                var filePath = Path.Combine(tempPath, file.FileName);
//                using (var stream = new FileStream(filePath, FileMode.Create))
//                {
//                    await file.CopyToAsync(stream);
//                }
//                inputPaths.Add(filePath);
//            }

//            string outputFile = Path.Combine(tempPath, $"merged_{Guid.NewGuid()}.mp3");

//            string ffmpegpath = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
//            FFmpeg.SetExecutablesPath(ffmpegpath);

//            var conversion = FFmpeg.Conversions.New();
//            foreach (var input in inputPaths)
//            {
//                conversion.AddParameter($"-i \"{input}\"", ParameterPosition.PreInput);
//            }

//            string filter = $"concat=n={inputPaths.Count}:v=0:a=1[out]";
//            conversion.AddParameter($"-filter_complex \"{filter}\" -map \"[out]\" \"{outputFile}\"");

//            await conversion.Start();

//            var fileBytes = await System.IO.File.ReadAllBytesAsync(outputFile);

//            var result = File(fileBytes, "audio/mpeg"); // ❌ No filename → preview allowed

//            // ✅ Cleanup in background
//            _ = Task.Run(() =>
//            {
//                try { Directory.Delete(tempPath, true); } catch { /* optional logging */ }
//            });

//            return result;
//        }



//        [HttpPost("MergeAudios")]
//        [Produces("audio/mpeg")]
//        [Consumes("multipart/form-data")]
//        public async Task<IActionResult> MergeAudios([FromForm(Name = "filesList")] List<IFormFile> audioFiles)
//        {
//            if (audioFiles == null || audioFiles.Count < 2)
//                return BadRequest("At least two audio files are required to merge.");

//            string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "TempAudio");
//            Directory.CreateDirectory(tempPath);

//            var inputPaths = new List<string>();
//            foreach (var file in audioFiles)
//            {
//                var filePath = Path.Combine(tempPath, file.FileName);
//                using (var stream = new FileStream(filePath, FileMode.Create))
//                {
//                    await file.CopyToAsync(stream);
//                }
//                inputPaths.Add(filePath);
//            }

//            string outputFile = Path.Combine(tempPath, $"merged_{Guid.NewGuid()}.mp3");

//            // Set FFmpeg path if required (once globally)
//            string ffmpegpath = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
//            FFmpeg.SetExecutablesPath(ffmpegpath); // optional if in PATH

//            // Merge audio files using concat filter
//            var conversion = FFmpeg.Conversions.New();

//            foreach (var input in inputPaths)
//            {
//                conversion.AddParameter($"-i \"{input}\"", ParameterPosition.PreInput);
//            }

//            string filter = $"concat=n={inputPaths.Count}:v=0:a=1[out]";
//            conversion.AddParameter($"-filter_complex \"{filter}\" -map \"[out]\" \"{outputFile}\"");

//            await conversion.Start();

//            var fileBytes = await System.IO.File.ReadAllBytesAsync(outputFile);

//            // Cleanup
//            Directory.Delete(tempPath, true);

//            //return File(fileBytes, "audio/mpeg", "merged_output.mp3");
//            return File(fileBytes, "audio/mpeg"); // ✅ no filename → no download

//        }

//    }
//}