using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Exceptions;
using FunctionalitiesWebAPI.Helper;
using System.Text.Json;

namespace FunctionalitiesWebAPI.Services
{
    public class VideoService : IVideoService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<VideoService> _logger;
        private readonly IVideoGenerator _videoGenerator;

        private static readonly Dictionary<string, string[]> AllowedMimeTypes = new()
        {
            { "image", new[] { "image/jpeg", "image/png", "image/webp" } },
            { "audio", new[] { "audio/mpeg", "audio/wav", "audio/x-wav", "audio/mp4", "audio/x-m4a", "audio/m4a", "audio/aac" } },
            { "video", new[] { "video/mp4", "video/quicktime", "video/x-matroska", "video/webm" } }
        };

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".m4a", ".aac"
        };

        private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov", ".mkv", ".webm"
        };


        public VideoService(
            IWebHostEnvironment env,
            ILogger<VideoService> logger,
            IVideoGenerator videoGenerator
        )
        {
            _env = env;
            _logger = logger;
            _videoGenerator = videoGenerator;
        }

        private string GetMediaFolder()
        {
            var mediaPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "media");
            Directory.CreateDirectory(mediaPath);
            return mediaPath;
        }

        private static void ValidateFile(IFormFile file, HashSet<string> allowedExtensions, string[] allowedMimeTypes, string typeName)
        {
            if (file == null || file.Length == 0)
                throw new MediaValidationException($"{typeName} file is required.");

            var ext = Path.GetExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(ext) || !allowedExtensions.Contains(ext))
                throw new MediaValidationException($"Invalid {typeName} file extension. Allowed: {string.Join(", ", allowedExtensions)}");

            if (string.IsNullOrWhiteSpace(file.ContentType) || !allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                throw new MediaValidationException($"Invalid {typeName} MIME type: {file.ContentType}");
        }

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var ext = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid() + ext;
            var path = Path.Combine(folder, fileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return path;
        }

        private void SafeDelete(string? path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp file: {file}", path);
            }
        }

        public async Task<string> GenerateVideoFromImageAndAudio(VideoUploadRequest request)
        {
            ValidateFile(request.Image, AllowedImageExtensions, AllowedMimeTypes["image"], "Image");
            ValidateFile(request.Audio, AllowedAudioExtensions, AllowedMimeTypes["audio"], "Audio");

            var folder = GetMediaFolder();

            string? imagePath = null;
            string? audioPath = null;

            try
            {
                imagePath = await SaveFileAsync(request.Image, folder);
                audioPath = await SaveFileAsync(request.Audio, folder);

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputVideoPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.GenerateVideoAsync(imagePath, audioPath, outputVideoPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(imagePath);
                SafeDelete(audioPath);
            }
        }

        public async Task<string> ReplaceVideoAudio(AudioVideoDto request)
        {
            ValidateFile(request.Video, AllowedVideoExtensions, AllowedMimeTypes["video"], "Video");
            ValidateFile(request.Audio, AllowedAudioExtensions, AllowedMimeTypes["audio"], "Audio");

            var folder = GetMediaFolder();

            string? videoPath = null;
            string? audioPath = null;

            try
            {
                videoPath = await SaveFileAsync(request.Video, folder);
                audioPath = await SaveFileAsync(request.Audio, folder);

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputVideoPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.GenerateVideoFromAudioAsync(videoPath, audioPath, outputVideoPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(videoPath);
                SafeDelete(audioPath);
            }
        }

        public async Task<string> MergeAudioWithVideo(AudioVideoDto request)
        {
            ValidateFile(request.Video, AllowedVideoExtensions, AllowedMimeTypes["video"], "Video");
            ValidateFile(request.Audio, AllowedAudioExtensions, AllowedMimeTypes["audio"], "Audio");

            var folder = GetMediaFolder();

            string? videoPath = null;
            string? audioPath = null;

            try
            {
                videoPath = await SaveFileAsync(request.Video, folder);
                audioPath = await SaveFileAsync(request.Audio, folder);

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputVideoPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.MergeAudioWithVideoAsync(videoPath, audioPath, outputVideoPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(videoPath);
                SafeDelete(audioPath);
            }
        }

        public async Task<string> CutVideo(VideoCutRequest request)
        {
            ValidateFile(request.Video, AllowedVideoExtensions, AllowedMimeTypes["video"], "Video");

            var folder = GetMediaFolder();

            string? inputPath = null;

            try
            {
                inputPath = await SaveFileAsync(request.Video, folder);

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.CutVideoAsync(inputPath, request.StartTime, request.EndTime, outputPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(inputPath);
            }
        }

        public async Task<string> ExtractAudio(VideoUploadDto request)
        {
            ValidateFile(request.Video, AllowedVideoExtensions, AllowedMimeTypes["video"], "Video");

            var folder = GetMediaFolder();

            string? videoPath = null;

            try
            {
                videoPath = await SaveFileAsync(request.Video, folder);

                var outputFileName = Guid.NewGuid() + ".m4a";
                var outputPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.ExtractAudioFromVideoAsync(videoPath, outputPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(videoPath);
            }
        }

        public async Task<string> GenerateImageTranstion(ImageTransitionRequest request)
        {
            if (request == null)
                throw new MediaValidationException("Request is required.");

            if (request.Meta == null || request.Meta.Count == 0)
                throw new MediaValidationException("Meta is required.");

            if (request.Images == null || request.Images.Count == 0)
                throw new MediaValidationException("At least 1 image is required.");

            ValidateFile(request.Audio, AllowedAudioExtensions, AllowedMimeTypes["audio"], "Audio");

            if (request.Meta.Count != request.Images.Count)
                throw new MediaValidationException("Meta count must match Images count.");

            var folder = GetMediaFolder();

            string? audioPath = null;
            var savedImages = new List<string>();

            try
            {
                audioPath = await SaveFileAsync(request.Audio, folder);

                var segments = new List<(string imagePath, int duration, string transition, double transitionDuration)>();

                for (int i = 0; i < request.Images.Count; i++)
                {
                    var img = request.Images[i];
                    var meta = request.Meta[i];

                    ValidateFile(img, AllowedImageExtensions, AllowedMimeTypes["image"], "Image");

                    if (meta.Duration <= 0)
                        throw new MediaValidationException("Each image duration must be > 0.");

                    if (meta.TransitionDuration < 0 || meta.TransitionDuration > meta.Duration)
                        throw new MediaValidationException("Transition duration must be <= duration.");

                    var imgPath = await SaveFileAsync(img, folder);
                    savedImages.Add(imgPath);

                    segments.Add((imgPath, meta.Duration, meta.Transition, meta.TransitionDuration));
                }

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputVideoPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.GenerateTimedImageVideoWithTransitionsAsync(segments, audioPath, outputVideoPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(audioPath);

                foreach (var img in savedImages)
                    SafeDelete(img);
            }
        }





        public async Task<string> GenerateFromTimedImages(TimedImageAudioRequest request)
        {
            if (request == null)
                throw new MediaValidationException("Request is required.");

            if (request.Images == null || request.Images.Count == 0)
                throw new MediaValidationException("At least 1 image is required.");

            if (request.Images.Any(x => x.Image == null))
                throw new MediaValidationException("All images must be provided.");

            ValidateFile(request.Audio, AllowedAudioExtensions, AllowedMimeTypes["audio"], "Audio");

            var folder = GetMediaFolder();

            string? audioPath = null;
            var savedImages = new List<string>();

            try
            {
                // Save audio
                audioPath = await SaveFileAsync(request.Audio, folder);

                // Build segments
                var segments = new List<(string imagePath, int duration)>();

                foreach (var item in request.Images)
                {
                    // Validate each image file
                    ValidateFile(item.Image, AllowedImageExtensions, AllowedMimeTypes["image"], "Image");

                    // Validate duration
                    if (item.Duration <= 0)
                        throw new MediaValidationException("Each image duration must be greater than 0 seconds.");

                    // Save image
                    var imgPath = await SaveFileAsync(item.Image, folder);
                    savedImages.Add(imgPath);

                    // Add to segments list
                    segments.Add((imgPath, item.Duration));
                }

                var outputFileName = Guid.NewGuid() + ".mp4";
                var outputVideoPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.GenerateTimedImageVideoAsync(segments, audioPath, outputVideoPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(audioPath);

                foreach (var img in savedImages)
                    SafeDelete(img);
            }
        }



        public async Task<string> SplitAndKeepAudio(AudioSplitRequest request)
        {
            if (request == null)
                throw new MediaValidationException("Request is required.");

            ValidateFile(request.Audio, AllowedAudioExtensions, AllowedMimeTypes["audio"], "Audio");

            if (request.Segments == null || request.Segments.Count == 0)
                throw new MediaValidationException("At least 1 segment is required.");

            var folder = GetMediaFolder();

            string? audioPath = null;

            try
            {
                audioPath = await SaveFileAsync(request.Audio, folder);

                var outputFileName = Guid.NewGuid() + ".mp3";
                var outputPath = Path.Combine(folder, outputFileName);

                await _videoGenerator.SplitAndKeepAudioAsync(audioPath, request.Segments, outputPath);

                return outputFileName;
            }
            finally
            {
                SafeDelete(audioPath);
            }
        }
    }
}
