using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Helper;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
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
        private readonly IWebHostEnvironment _environment;

        public MediaManipulationController(IWebHostEnvironment environment, ILogger<MediaManipulationController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("CompressVideos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompressVideos([FromForm] UploadDto upload)
        {
            var allowedVideoTypes = new[] { "video/mp4", "video/x-m4v", "video/webm", "video/x-msvideo", "video/x-ms-wmv", "video/quicktime" };
            if (FileHelper.ValidateFile(upload.File, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult error)
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

        [HttpPost("HighQualityVideos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> HighQualityVideos([FromForm] UploadDto upload)
        {
            var allowedVideoTypes = new[]
            {
        "video/mp4",
        "video/x-m4v",
        "video/webm",
        "video/x-msvideo",
        "video/x-ms-wmv",
        "video/quicktime"
    };

            if (FileHelper.ValidateFile(upload.File, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult error)
                return error;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "videos");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var inputFileName = $"{Guid.NewGuid():N}.mp4";
            var outputFileName = $"compressed_{Guid.NewGuid():N}.mp4";

            var inputPath = Path.Combine(uploadsFolder, inputFileName);
            var outputPath = Path.Combine(uploadsFolder, outputFileName);

            try
            {
                await FileHelper.SaveFileAsync(upload.File, inputPath);

                await MediaManipulationHelper.HighMediaVideo(inputPath, outputPath);

                return PhysicalFile(outputPath, "video/mp4", outputFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video compression failed.");
                return Problem("Video compression failed.", statusCode: 500);
            }
        }

        [HttpPost("HighQualityVideosOld")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> HighQualityVideosOld([FromForm] UploadDto upload)
        {
            var allowedVideoTypes = new[]
            {
        "video/mp4",
        "video/x-m4v",
        "video/webm",
        "video/x-msvideo",
        "video/x-ms-wmv",
        "video/quicktime"
    };

            if (FileHelper.ValidateFile(upload.File, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult error)
                return error;

            var inputPath = FileHelper.GenerateTempPath(".mp4");
            var outputPath = FileHelper.GenerateTempPath("_compressed.mp4");

            try
            {
                await FileHelper.SaveFileAsync(upload.File, inputPath);

                await MediaManipulationHelper.HighMediaVideo(inputPath, outputPath);

                var downloadName = $"compressed_{Guid.NewGuid():N}.mp4";

                var result = PhysicalFile(outputPath, "video/mp4", downloadName);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        FileHelper.SafeDelete(outputPath);
                    }
                    catch { }
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video compression failed.");
                return Problem("Video compression failed.", statusCode: 500);
            }
            finally
            {
                FileHelper.SafeDelete(inputPath);
            }
        }

        [HttpPost("CompressAudio")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompressAudio([FromForm] UploadDto upload)
        {
            var allowedAudioTypes = new[] { "audio/mpeg", "audio/mp3", "audio/x-m4a", "audio/wav", "audio/aac" };

            if (FileHelper.ValidateFile(upload.File, 50 * 1024 * 1024, "Audio", allowedAudioTypes) is IActionResult error)
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

            if (FileHelper.ValidateFile(upload.File, 10 * 1024 * 1024, "Image", allowedImageTypes) is IActionResult error)
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
            var allowedVideoTypes = new[]
            {
        "video/mp4",
        "video/x-m4v",
        "video/webm",
        "video/x-msvideo",
        "video/quicktime"
    };

            if (FileHelper.ValidateFileCount(filesList, 2, "video files") is IActionResult error)
                return error;

            string uploadDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Storage",
                "uploads",
                Guid.NewGuid().ToString()
            );

            Directory.CreateDirectory(uploadDir);

            try
            {
                List<string> savedFilePaths = new();

                foreach (var file in filesList)
                {
                    if (FileHelper.ValidateFile(file, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult err)
                        return err;

                    string filePath = Path.Combine(uploadDir, Path.GetFileName(file.FileName));
                    await FileHelper.SaveFileAsync(file, filePath);
                    savedFilePaths.Add(filePath);
                }

                string mergedFilePath = await MediaManipulationHelper.MergeVideo(uploadDir, savedFilePaths);

                if (!System.IO.File.Exists(mergedFilePath))
                    return StatusCode(500, "Merged video not created.");

                var stream = new FileStream(mergedFilePath, FileMode.Open, FileAccess.Read);
                return File(stream, "video/mp4", "merged.mp4");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    details = ex.Message
                });
            }
        }

        [HttpPost("MergeVideosUnrecognized")]
        [Consumes("multipart/form-data")]
        [Produces("video/mp4")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MergeVideosUnrecognized([FromForm] List<IFormFile> filesList)
        {
            var allowedVideoTypes = new[]
            {
        "video/mp4",
        "video/x-m4v",
        "video/webm",
        "video/x-msvideo",
        "video/quicktime"
    };

            if (FileHelper.ValidateFileCount(filesList, 2, "video files") is IActionResult error)
                return error;

            string uploadDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Storage",
                "uploads",
                Guid.NewGuid().ToString()
            );

            Directory.CreateDirectory(uploadDir);

            try
            {
                List<string> savedFilePaths = new();

                foreach (var file in filesList)
                {
                    if (FileHelper.ValidateFile(file, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult err)
                        return err;

                    string safeFileName = Path.GetFileName(file.FileName);
                    string filePath = Path.Combine(uploadDir, safeFileName);

                    await FileHelper.SaveFileAsync(file, filePath);
                    savedFilePaths.Add(filePath);
                }

                string mergedFilePath = await MediaManipulationHelper.MergeVideo(uploadDir, savedFilePaths);

                if (!System.IO.File.Exists(mergedFilePath))
                    return StatusCode(500, "Merged video not created.");

                // ✅ Swagger-friendly download
                //var bytes = await System.IO.File.ReadAllBytesAsync(mergedFilePath);
                //return File(bytes, "video/mp4", "merged.mp4");
                var stream = new FileStream(mergedFilePath, FileMode.Open, FileAccess.Read);
                return File(stream, "video/mp4", "merged.mp4");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    details = ex.Message
                });
            }
        }



        [HttpPost("MergeVideosOld")]
        [Consumes("multipart/form-data")]
        [Produces("video/mp4")]
        public async Task<IActionResult> MergeVideos123([FromForm] List<IFormFile> filesList)
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
                var stream = new FileStream(mergedFilePath, FileMode.Open, FileAccess.Read);
                return File(stream, "video/mp4", "merged.mp4");
                //return PhysicalFile(mergedFilePath, "video/mp4", "merged.mp4");
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

        [HttpPost("NotWorkExtractAudioFromVideo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExtractAudioFromVideo([FromForm] UploadDto upload)
        {
            var allowedVideoTypes = new[]
            {
        "video/mp4",
        "video/x-m4v",
        "video/webm",
        "video/x-msvideo",
        "video/x-ms-wmv",
        "video/quicktime"
    };

            if (FileHelper.ValidateFile(upload.File, 100 * 1024 * 1024, "Video", allowedVideoTypes) is IActionResult error)
                return error;

            var inputPath = FileHelper.GenerateTempPath(Path.GetExtension(upload.File.FileName));
            var outputPath = FileHelper.GenerateTempPath("_audio.mp3");

            try
            {
                await FileHelper.SaveFileAsync(upload.File, inputPath);

                string ffmpegpath = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
                FFmpeg.SetExecutablesPath(ffmpegpath);

                // Extract audio only
                var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(inputPath, outputPath);

                // Force mp3 output
                conversion.AddParameter("-vn -acodec libmp3lame -b:a 128k", ParameterPosition.PostInput);

                await conversion.Start();

                var fileBytes = await FileHelper.ReadFileAsBytesAsync(outputPath);
                return File(fileBytes, "audio/mpeg", "audio.mp3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio extraction from video failed.");
                return Problem("Audio extraction from video failed.", statusCode: 500);
            }
            finally
            {
                FileHelper.SafeDelete(inputPath);
                FileHelper.SafeDelete(outputPath);
            }
        }


        [HttpPost("ExtractAudio")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExtractAudio([FromForm] UploadDto upload)
        {
            var allowedVideoTypes = new[]
            {
            "video/mp4",
            "video/x-m4v",
            "video/webm",
            "video/x-msvideo",
            "video/x-ms-wmv",
            "video/quicktime"
        };

            if (upload.File == null || upload.File.Length == 0)
                return BadRequest("No file uploaded.");

            if (!allowedVideoTypes.Contains(upload.File.ContentType))
                return BadRequest("Invalid video format.");

            if (upload.File.Length > 100 * 1024 * 1024)
                return BadRequest("File size exceeds 100MB limit.");

            string inputPath = string.Empty;

            try
            {
                // 1️⃣ Create temp input file
                inputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(upload.File.FileName));

                using (var stream = new FileStream(inputPath, FileMode.Create))
                {
                    await upload.File.CopyToAsync(stream);
                }

                // 2️⃣ Create permanent audio folder
                var audioFolder = Path.Combine(_environment.WebRootPath, "audios");

                if (!Directory.Exists(audioFolder))
                    Directory.CreateDirectory(audioFolder);

                var outputFileName = Guid.NewGuid() + ".mp3";
                var outputPath = Path.Combine(audioFolder, outputFileName);

                // 3️⃣ Set FFmpeg path
                string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
                FFmpeg.SetExecutablesPath(ffmpegPath);

                // 4️⃣ Extract audio
                var conversion = await FFmpeg.Conversions.FromSnippet.ExtractAudio(inputPath, outputPath);
                conversion.AddParameter("-vn -acodec libmp3lame -b:a 128k", ParameterPosition.PostInput);

                await conversion.Start();

                // 5️⃣ Return URL instead of file bytes
                var fileUrl = $"{Request.Scheme}://{Request.Host}/audios/{outputFileName}";

                return Ok(new
                {
                    message = "Audio extracted successfully",
                    audioUrl = fileUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio extraction failed.");
                return StatusCode(500, "Audio extraction failed.");
            }
            finally
            {
                // Delete only input temp file
                if (!string.IsNullOrEmpty(inputPath) && System.IO.File.Exists(inputPath))
                    System.IO.File.Delete(inputPath);
            }
        }

        [HttpPost("CreateSlideshow")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateSlideshow([FromForm] SlideshowDto model)
        {
            if (model.Images == null || !model.Images.Any())
                return BadRequest("Please upload images.");

            if (model.Audio == null)
                return BadRequest("Please upload audio.");

            // Create temporary folder
            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                // 1️⃣ Save Images
                List<string> imagePaths = new List<string>();
                int index = 0;
                foreach (var image in model.Images)
                {
                    var path = Path.Combine(tempFolder, $"img{index}.jpg");
                    using var stream = new FileStream(path, FileMode.Create);
                    await image.CopyToAsync(stream);
                    imagePaths.Add(path);
                    index++;
                }

                // 2️⃣ Save Audio
                var audioPath = Path.Combine(tempFolder, "audio.mp3");
                using (var stream = new FileStream(audioPath, FileMode.Create))
                {
                    await model.Audio.CopyToAsync(stream);
                }

                // 3️⃣ Prepare output folder
                var outputFolder = Path.Combine(_environment.WebRootPath, "videos");
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputPath = Path.Combine(outputFolder, outputFileName);

                // 4️⃣ Set FFmpeg Path
                string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
                FFmpeg.SetExecutablesPath(ffmpegPath);

                // 5️⃣ Build FFmpeg conversion
                var conversion = FFmpeg.Conversions.New();

                int durationPerImage = 3; // seconds per image

                // Add each image as a video stream
                for (int i = 0; i < imagePaths.Count; i++)
                {
                    conversion.AddParameter($"-loop 1 -t {durationPerImage} -i \"{imagePaths[i]}\"", ParameterPosition.PreInput);
                }

                // Add audio input
                conversion.AddParameter($"-i \"{audioPath}\"", ParameterPosition.PreInput);

                // Build filter_complex for concatenation
                string filter = "";
                for (int i = 0; i < imagePaths.Count; i++)
                {
                    filter += $"[{i}:v]scale=1280:720,setsar=1[v{i}];";
                }

                for (int i = 0; i < imagePaths.Count; i++)
                {
                    filter += $"[v{i}]";
                }

                filter += $"concat=n={imagePaths.Count}:v=1:a=0[vout]";

                // Final FFmpeg parameters
                conversion.AddParameter($"-filter_complex \"{filter}\" -map \"[vout]\" -map {imagePaths.Count}:a -shortest -pix_fmt yuv420p -c:v libx264 -c:a aac");

                conversion.SetOutput(outputPath);

                // 6️⃣ Start conversion
                await conversion.Start();

                // 7️⃣ Return video URL
                var videoUrl = $"{Request.Scheme}://{Request.Host}/videos/{outputFileName}";

                return Ok(new
                {
                    message = "Slideshow created successfully",
                    videoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slideshow creation failed.");
                return StatusCode(500, "Slideshow creation failed.");
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }
        }

        [HttpPost("CreateCurlSlideshow")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCurlSlideshow([FromForm] SlideshowDto model)
        {
            if (model.Images == null || !model.Images.Any())
                return BadRequest("Please upload images.");

            if (model.Audio == null)
                return BadRequest("Please upload audio.");

            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                // 1️⃣ Save Images
                List<string> imagePaths = new();
                int index = 0;

                foreach (var image in model.Images)
                {
                    var path = Path.Combine(tempFolder, $"img{index}.jpg");

                    await using var stream = new FileStream(path, FileMode.Create);
                    await image.CopyToAsync(stream);

                    imagePaths.Add(path);
                    index++;
                }

                // 2️⃣ Save Audio
                var audioPath = Path.Combine(tempFolder, "audio.mp3");
                await using (var stream = new FileStream(audioPath, FileMode.Create))
                    await model.Audio.CopyToAsync(stream);

                // 3️⃣ Output Folder
                var outputFolder = Path.Combine(_environment.WebRootPath, "videos");
                Directory.CreateDirectory(outputFolder);

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputPath = Path.Combine(outputFolder, outputFileName);

                // 4️⃣ FFmpeg Path
                string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
                FFmpeg.SetExecutablesPath(ffmpegPath);

                var conversion = FFmpeg.Conversions.New();

                int durationPerImage = 3;   // seconds visible
                int transitionDuration = 1; // curl duration

                // 5️⃣ Add image inputs
                for (int i = 0; i < imagePaths.Count; i++)
                {
                    conversion.AddParameter(
                        $"-loop 1 -t {durationPerImage + transitionDuration} -i \"{imagePaths[i]}\"",
                        ParameterPosition.PreInput);
                }

                // Add audio input
                conversion.AddParameter($"-i \"{audioPath}\"", ParameterPosition.PreInput);

                // 6️⃣ Build filter_complex
                string filter = "";

                // Scale all images
                for (int i = 0; i < imagePaths.Count; i++)
                {
                    filter += $"[{i}:v]scale=1280:720,setsar=1[v{i}];";
                }

                string current = "[v0]";
                double offset = durationPerImage;
                string finalLabel = "vfinal";

                // Chain pagecurl transitions
                for (int i = 1; i < imagePaths.Count; i++)
                {
                    string nextLabel = i == imagePaths.Count - 1
                        ? finalLabel
                        : $"vxf{i}";

                    filter += $"{current}[v{i}]xfade=transition=pagecurl:" +
                              $"duration={transitionDuration}:" +
                              $"offset={offset}[{nextLabel}];";

                    current = $"[{nextLabel}]";
                    offset += durationPerImage;
                }

                filter = filter.TrimEnd(';');

                conversion.AddParameter(
                    $"-filter_complex \"{filter}\" " +
                    $"-map \"[{finalLabel}]\" -map {imagePaths.Count}:a " +
                    "-shortest -pix_fmt yuv420p -c:v libx264 -c:a aac -r 25 -y"
                );

                conversion.SetOutput(outputPath);

                // 7️⃣ Run FFmpeg
                await conversion.Start();

                var videoUrl = $"{Request.Scheme}://{Request.Host}/videos/{outputFileName}";

                return Ok(new
                {
                    message = "Curl slideshow created successfully",
                    videoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slideshow creation failed.");
                return StatusCode(500, "Slideshow creation failed.");
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }
        }


        [HttpPost("CreateCinematicSlideshow")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCinematicSlideshow([FromForm] SlideshowDto model)
        {
            if (model.Images == null || !model.Images.Any())
                return BadRequest("Please upload images.");

            if (model.Audio == null)
                return BadRequest("Please upload audio.");

            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                // 1️⃣ Save Images
                List<string> imagePaths = new();
                int index = 0;
                foreach (var image in model.Images)
                {
                    var path = Path.Combine(tempFolder, $"img{index}{Path.GetExtension(image.FileName)}");
                    await using var stream = new FileStream(path, FileMode.Create);
                    await image.CopyToAsync(stream);
                    imagePaths.Add(path);
                    index++;
                }

                // 2️⃣ Save Audio
                var audioExt = Path.GetExtension(model.Audio!.FileName);
                var audioPath = Path.Combine(tempFolder, "audio" + audioExt);
                await using (var stream = new FileStream(audioPath, FileMode.Create))
                    await model.Audio.CopyToAsync(stream);

                // 3️⃣ FFmpeg Setup
                string ffmpegFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
                string ffprobeExe = Path.Combine(ffmpegFolder, "ffprobe.exe");

                if (!System.IO.File.Exists(ffprobeExe))
                    throw new FileNotFoundException("FFprobe not found", ffprobeExe);

                FFmpeg.SetExecutablesPath(ffmpegFolder);

                // 4️⃣ Get Audio Duration
                double audioDuration = 10; // default
                var startInfo = new ProcessStartInfo
                {
                    FileName = ffprobeExe,
                    Arguments = $"-i \"{audioPath}\" -show_entries format=duration -v quiet -of csv=\"p=0\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new Exception("Failed to start ffprobe.");

                    string result = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (double.TryParse(result.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                    {
                        audioDuration = Math.Round(parsed, 3);

                        // ✅ Optional TimeSpan object
                        var audioTimeSpan = TimeSpan.FromSeconds(audioDuration);
                        _logger.LogInformation($"Audio Duration: {audioDuration} seconds ({audioTimeSpan})");
                    }
                }

                // 5️⃣ Timing Calculations
                int imageCount = imagePaths.Count;
                double transitionDuration = 1;
                double perImageDuration = Math.Max(audioDuration / imageCount, 2);

                // 6️⃣ Output Folder
                var outputFolder = Path.Combine(_environment.WebRootPath, "videos");
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputPath = Path.Combine(outputFolder, outputFileName);

                // 7️⃣ Build Conversion
                var conversion = FFmpeg.Conversions.New();
                for (int i = 0; i < imagePaths.Count; i++)
                {
                    conversion.AddParameter(
                        $"-loop 1 -t {(perImageDuration + transitionDuration).ToString(CultureInfo.InvariantCulture)} -i \"{imagePaths[i]}\"",
                        ParameterPosition.PreInput);
                }
                conversion.AddParameter($"-i \"{audioPath}\"", ParameterPosition.PreInput);

                // 8️⃣ Build Filter Graph
                List<string> labels = new();
                string filter = "";

                for (int i = 0; i < imagePaths.Count; i++)
                {
                    filter += $"[{i}:v]scale=1280:720,setsar=1," +
                              $"zoompan=z='zoom+0.001':d={(int)((perImageDuration + transitionDuration) * 25)}:s=1280x720[v{i}];";
                    labels.Add($"[v{i}]");
                }

                string current = labels[0];
                double offset = perImageDuration;
                string finalLabel = "vfinal";

                string[] effects =
                {
            "fade","wipeleft","wiperight","slideleft",
            "slideright","circleopen","circleclose",
            "dissolve","smoothleft","smoothright"
        };

                for (int i = 1; i < labels.Count; i++)
                {
                    string nextOutput = i == labels.Count - 1 ? finalLabel : $"vxf{i}";
                    string effect = effects[i % effects.Length];

                    // ✅ Use plain decimal seconds for FFmpeg
                    string offsetDecimal = offset.ToString("0.###", CultureInfo.InvariantCulture);

                    filter += $"{current}{labels[i]}xfade=transition={effect}:" +
                              $"duration={transitionDuration.ToString(CultureInfo.InvariantCulture)}:" +
                              $"offset={offsetDecimal}[{nextOutput}];";

                    current = $"[{nextOutput}]";
                    offset += perImageDuration;
                }

                filter = filter.TrimEnd(';');

                conversion.AddParameter(
                    $"-filter_complex \"{filter}\" " +
                    $"-map \"[{finalLabel}]\" -map {imagePaths.Count}:a " +
                    "-shortest -pix_fmt yuv420p -c:v libx264 -c:a aac -r 25 -y"
                );

                conversion.SetOutput(outputPath);

                conversion.OnProgress += (s, args) =>
                {
                    _logger.LogInformation($"FFmpeg Progress: {args.Duration}/{args.TotalLength}");
                };

                // 9️⃣ Start Conversion
                await conversion.Start();

                // 🔟 Return URL
                var videoUrl = $"{Request.Scheme}://{Request.Host}/videos/{outputFileName}";
                return Ok(new
                {
                    message = "Cinematic slideshow created successfully",
                    videoUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cinematic slideshow creation failed.");
                return StatusCode(500, "Cinematic slideshow creation failed.");
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }
        }

        
        //[HttpPost("convert-mpeg-to-mp3")]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> ConvertMpegToMp3([FromForm] IFormFile file)
        //{
        //    var folder = Path.Combine(Directory.GetCurrentDirectory(), "Media");
        //    Directory.CreateDirectory(folder);

        //    var inputPath = Path.Combine(folder, file.FileName);

        //    using (var stream = new FileStream(inputPath, FileMode.Create))
        //    {
        //        await file.CopyToAsync(stream);
        //    }

        //    var outputPath = await MediaManipulationHelper.ConvertMpegToMp3(inputPath);

        //    var bytes = await System.IO.File.ReadAllBytesAsync(outputPath);

        //    return File(bytes, "audio/mpeg", Path.GetFileName(outputPath));
        //}
    }
}
