using FunctionalitiesWebAPI.DTO;
using System.Diagnostics;

namespace FunctionalitiesWebAPI.Helper
{
    public class VideoGenerators : IVideoGenerator
    {
        public async Task GenerateVideoAsync(string imagePath, string audioPath, string outputVideoPath)
        {
            // Image + Audio => Video
            var args =
                $"-loop 1 -i \"{imagePath}\" -i \"{audioPath}\" " +
                "-c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p " +
                "-c:a aac -shortest -y " +
                $"\"{outputVideoPath}\"";

            await RunFFmpegAsync(args);
        }

        public async Task GenerateVideoFromAudioAsync(string videoPath, string audioPath, string outputVideoPath)
        {
            // Video + Audio => New Video
            var args =
                $"-i \"{videoPath}\" -i \"{audioPath}\" " +
                "-map 0:v:0 -map 1:a:0 " +
                "-c:v copy -c:a aac -shortest -y " +
                $"\"{outputVideoPath}\"";

            await RunFFmpegAsync(args);
        }

        public async Task MergeAudioWithVideoAsync(string videoPath, string audioPath, string outputVideoPath)
        {
            // Same as above (merge audio with video)
            var args =
                $"-i \"{videoPath}\" -i \"{audioPath}\" " +
                "-map 0:v:0 -map 1:a:0 " +
                "-c:v copy -c:a aac -shortest -y " +
                $"\"{outputVideoPath}\"";

            await RunFFmpegAsync(args);
        }

        public async Task ExtractAudioFromVideoAsync(string videoPath, string outputAudioPath)
        {
            // Extract audio only
            var args =
                $"-i \"{videoPath}\" -vn -acodec mp3 -y " +
                $"\"{outputAudioPath}\"";

            await RunFFmpegAsync(args);
        }

        public async Task CutVideoAsync(string inputPath, string startTime, string endTime, string outputPath)
        {
            // Cut video by start and end time
            // Example: startTime = "00:00:10", endTime = "00:00:30"
            var args =
                $"-i \"{inputPath}\" -ss {startTime} -to {endTime} -c copy -y " +
                $"\"{outputPath}\"";

            await RunFFmpegAsync(args);
        }

        public async Task GenerateTimedImageVideoAsync(
            List<(string imagePath, int duration)> segments,
            string audioPath,
            string outputVideoPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var inputListPath = Path.Combine(tempDir, "input.txt");
            var videoSegments = new List<string>();

            try
            {
                int index = 0;

                foreach (var (imagePath, duration) in segments)
                {
                    var segmentPath = Path.Combine(tempDir, $"segment_{index}.mp4");

                    var ffmpegArgs =
                        $"-loop 1 -i \"{imagePath}\" -t {duration} " +
                        "-vf scale=1280:720 -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -y " +
                        $"\"{segmentPath}\"";

                    await RunFFmpegAsync(ffmpegArgs);

                    videoSegments.Add(segmentPath);
                    index++;
                }

                await File.WriteAllLinesAsync(inputListPath, videoSegments.Select(v => $"file '{v}'"));

                var concatOutputPath = Path.Combine(tempDir, "combined.mp4");
                var concatArgs =
                    $"-f concat -safe 0 -i \"{inputListPath}\" -c copy -y " +
                    $"\"{concatOutputPath}\"";

                await RunFFmpegAsync(concatArgs);

                var finalArgs =
                    $"-i \"{concatOutputPath}\" -i \"{audioPath}\" " +
                    "-c:v copy -c:a aac -shortest -y " +
                    $"\"{outputVideoPath}\"";

                await RunFFmpegAsync(finalArgs);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
        }

        public async Task SplitAndKeepAudioAsync(string audioPath, List<AudioSegmentDto> segments, string outputPath)
        {
            // This will create multiple audio files (or one final merged file depending on your need)
            // Here: create a folder and export segments separately.

            var outputDir = Path.Combine(Path.GetDirectoryName(outputPath)!, "audio_segments");
            Directory.CreateDirectory(outputDir);

            int index = 1;

            foreach (var segment in segments)
            {
                var segmentOutput = Path.Combine(outputDir, $"segment_{index}.mp3");

                var args =
                    $"-i \"{audioPath}\" -ss {segment.Start} -to {segment.End} -c copy -y " +
                    $"\"{segmentOutput}\"";

                await RunFFmpegAsync(args);
                index++;
            }
        }

        public async Task GenerateTimedImageVideoWithTransitionsAsync(
    List<(string imagePath, int duration, string transition, double transitionDuration)> segments,
    string audioPath,
    string outputVideoPath)
        {
            if (segments == null || segments.Count == 0)
                throw new Exception("At least 1 image segment is required.");

            // 1) Build ffmpeg inputs
            var inputArgs = new List<string>();

            for (int i = 0; i < segments.Count; i++)
            {
                inputArgs.Add($"-loop 1 -t {segments[i].duration} -i \"{segments[i].imagePath}\"");
            }

            // Add audio input at the end
            inputArgs.Add($"-i \"{audioPath}\"");

            // 2) Build filter_complex using xfade
            // First we normalize each image stream to same size and fps
            var filter = new List<string>();

            for (int i = 0; i < segments.Count; i++)
            {
                filter.Add(
                    $"[{i}:v]scale=1280:720,fps=30,format=yuv420p[v{i}]"
                );
            }

            // 3) Create xfade chain
            // Offset is when transition starts
            // offset = sum(previous durations) - sum(previous transition durations)
            double offset = segments[0].duration;

            string lastStream = $"[v0]";

            for (int i = 1; i < segments.Count; i++)
            {
                var transition = segments[i].transition ?? "fade";
                var transDur = segments[i].transitionDuration;

                // transition must start before previous ends
                offset = offset - transDur;

                var outStream = $"[vxf{i}]";

                filter.Add(
                    $"{lastStream}[v{i}]xfade=transition={transition}:duration={transDur}:offset={offset}{outStream}"
                );

                lastStream = outStream;

                offset += segments[i].duration;
            }

            // 4) Final args
            var args =
                $"{string.Join(" ", inputArgs)} " +
                $"-filter_complex \"{string.Join(";", filter)}\" " +
                $"-map \"{lastStream}\" -map {segments.Count}:a:0 " +
                $"-c:v libx264 -preset fast -crf 18 -c:a aac -b:a 192k " +
                $"-pix_fmt yuv420p -shortest -y \"{outputVideoPath}\"";

            await RunFFmpegAsync(args);
        }


        private async Task RunFFmpegAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg", // If in PATH. Else give full path
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
