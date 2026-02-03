using System.Diagnostics;

namespace FunctionalitiesWebAPI.Helper
{
    public class VideoGenerators : IVideoGenerator
    {
        public async Task GenerateTimedImageVideoAsync(List<(string imagePath, int duration)> segments, string audioPath, string outputPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var inputListPath = Path.Combine(tempDir, "input.txt");
            var videoSegments = new List<string>();

            try
            {
                int index = 0;

                // Generate a video from each image with its duration
                foreach (var (imagePath, duration) in segments)
                {
                    var segmentPath = Path.Combine(tempDir, $"segment_{index}.mp4");

                    var ffmpegArgs = $"-loop 1 -i \"{imagePath}\" -t {duration} -vf scale=1280:720 -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -y \"{segmentPath}\"";
                    await RunFFmpegAsync(ffmpegArgs);

                    videoSegments.Add(segmentPath);
                    index++;
                }

                // Create input.txt for FFmpeg concat
                await File.WriteAllLinesAsync(inputListPath, videoSegments.Select(v => $"file '{v}'"));

                // Concatenate segments into one video
                var concatOutputPath = Path.Combine(tempDir, "combined.mp4");
                var concatArgs = $"-f concat -safe 0 -i \"{inputListPath}\" -c copy -y \"{concatOutputPath}\"";
                await RunFFmpegAsync(concatArgs);

                // Merge audio with the final video
                var finalArgs = $"-i \"{concatOutputPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -shortest -y \"{outputPath}\"";
                await RunFFmpegAsync(finalArgs);
            }
            finally
            {
                // Clean up temp folder
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
                }
            }
        }

        private async Task RunFFmpegAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg", // Or full path to ffmpeg.exe
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed: {stderr}");
            }
        }
    }
}
