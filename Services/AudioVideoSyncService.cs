using FunctionalitiesWebAPI.Helper;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text.Json;

namespace FunctionalitiesWebAPI.Services
{
    public class AudioVideoSyncService : IAudioVideoSyncService
    {
        private readonly IFFmpegProcessor _ffmpeg;

        public AudioVideoSyncService(IFFmpegProcessor ffmpeg)
        {
            _ffmpeg = ffmpeg;
        }

        private string EnsureFolder(string subFolder)
        {
            string folder = Path.Combine(AppContext.BaseDirectory, subFolder);
            Directory.CreateDirectory(folder);
            return folder;
        }

        // -----------------------------------------------------------------------
        // SYNC SINGLE AUDIO WITH MULTIPLE VIDEOS
        // -----------------------------------------------------------------------
        public async Task<List<string>> SyncSingleAudioMultipleVideos(IFormFile audio, List<IFormFile> videos)
        {
            string uploadFolder = EnsureFolder("uploads");
            string outputFolder = EnsureFolder("outputs");

            string audioPath = Path.Combine(uploadFolder, audio.FileName);
            using (var stream = new FileStream(audioPath, FileMode.Create))
                await audio.CopyToAsync(stream);

            List<string> outputFiles = new();
            double audioOffset = 0; // keep track of how much audio has been used

            foreach (var video in videos)
            {
                if (!video.ContentType.StartsWith("video/"))
                    continue;

                string videoPath = Path.Combine(uploadFolder, video.FileName);
                using (var stream = new FileStream(videoPath, FileMode.Create))
                    await video.CopyToAsync(stream);

                string outputFile = Path.Combine(outputFolder, $"synced_{video.FileName}.mp4");

                // Use FFmpeg to take audio starting from current offset
                // -ss = start time, -t = duration (match video duration)
                string ffmpegArgs =
                    $"-ss {audioOffset} -i \"{audioPath}\" -i \"{videoPath}\" " +
                    $"-map 1:v -map 0:a -c:v copy -c:a aac -shortest \"{outputFile}\"";

                await _ffmpeg.RunCommand(ffmpegArgs);

                // Get video duration to update audio offset
                double videoDuration = await GetVideoDuration(videoPath); // implement helper
                audioOffset += videoDuration;

                outputFiles.Add(outputFile);
            }

            return outputFiles;
        }

        private async Task<double> GetVideoDuration(string videoPath)
        {
            // FFmpeg/FFprobe command to get video duration in seconds
            string args = $"-v error -select_streams v:0 -show_entries stream=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"";

            string output = await _ffmpeg.RunCommandWithOutput(args); // make sure your IFFmpegProcessor can return stdout

            if (double.TryParse(output.Trim(), out double duration))
                return duration;

            // fallback if parsing fails
            return 0;
        }       



        // -----------------------------------------------------------------------
        // STRETCH AUDIO TO MATCH VIDEO LENGTH
        // -----------------------------------------------------------------------
        public async Task<string> StretchAudioToMatchVideo(IFormFile audio, IFormFile video)
        {
            string uploadFolder = EnsureFolder("uploads");
            string outputFolder = EnsureFolder("outputs");

            // Save audio
            string audioPath = Path.Combine(uploadFolder, audio.FileName);
            using (var stream = new FileStream(audioPath, FileMode.Create))
                await audio.CopyToAsync(stream);

            // Save video
            string videoPath = Path.Combine(uploadFolder, video.FileName);
            using (var stream = new FileStream(videoPath, FileMode.Create))
                await video.CopyToAsync(stream);

            // Output
            string outputFile = Path.Combine(outputFolder, $"stretched_{video.FileName}.mp4");

            // FFmpeg stretch audio to match video duration
            string ffmpegArgs =
                $"-i \"{audioPath}\" -i \"{videoPath}\" -filter_complex \"[0:a]apad,aresample=async=1[a]\" -map 1:v -map \"[a]\" -c:v copy -shortest \"{outputFile}\"";

            await _ffmpeg.RunCommand(ffmpegArgs);

            return outputFile;
        }

        // -----------------------------------------------------------------------
        // SCRIPT-BASED SYNC (JSON + MULTIPLE VIDEOS)
        // -----------------------------------------------------------------------
        public async Task<List<string>> ScriptBasedSync(IFormFile audio, List<ScriptItem> scriptJson, List<IFormFile> videos)
        {
            string uploadFolder = EnsureFolder("uploads");
            string outputFolder = EnsureFolder("outputs");

            // Save audio
            string audioPath = Path.Combine(uploadFolder, audio.FileName);
            using (var stream = new FileStream(audioPath, FileMode.Create))
                await audio.CopyToAsync(stream);

            // Deserialize JSON array
            //var scriptItems = JsonSerializer.Deserialize<List<ScriptItem>>(scriptJson);
            //if (scriptItems == null || !scriptItems.Any())
            //    throw new Exception("ScriptJson is empty or invalid");

            List<string> outputs = new();

            foreach (var video in videos)
            {
                if (!video.ContentType.StartsWith("video/")) continue;

                string videoPath = Path.Combine(uploadFolder, video.FileName);
                using (var stream = new FileStream(videoPath, FileMode.Create))
                    await video.CopyToAsync(stream);

                var script = scriptJson.FirstOrDefault(s => s.VideoFileName == video.FileName);
                if (script == null) continue;

                string outputFile = Path.Combine(outputFolder, $"script_synced_{video.FileName}.mp4");

                // FFmpeg arguments
                string durationArg = script.AudioDuration.HasValue ? $"-t {script.AudioDuration.Value}" : "";
                string ffmpegArgs =
                    $"-ss {script.AudioStart} -i \"{audioPath}\" {durationArg} -i \"{videoPath}\" " +
                    $"-map 1:v -map 0:a -c:v copy -c:a aac -shortest \"{outputFile}\"";

                await _ffmpeg.RunCommand(ffmpegArgs);
                outputs.Add(outputFile);
            }

            return outputs;
        }

    }
}
