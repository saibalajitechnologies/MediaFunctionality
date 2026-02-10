using FunctionalitiesWebAPI.DTO;
using System.Diagnostics;

namespace FunctionalitiesWebAPI.Helper
{
    public class VideoGenerator : IVideoGenerator
    {
        private async Task RunFfmpegAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = CommonHelper.GetFfmpegExecutable(),
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);

            if (process == null)
                throw new Exception("Failed to start FFmpeg process.");

            var error = await process.StandardError.ReadToEndAsync();
            var output = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed.\n{error}\n{output}");
            }
        }

        public async Task GenerateVideoAsync(string imagePath, string audioPath, string outputVideoPath)
        {
            var args =
                $"-loop 1 -i \"{imagePath}\" -i \"{audioPath}\" " +
                $"-c:v libx264 -tune stillimage -c:a aac -b:a 192k " +
                $"-pix_fmt yuv420p -shortest \"{outputVideoPath}\"";

            await RunFfmpegAsync(args);
        }

        public async Task GenerateVideoFromAudioAsync(string videoPath, string audioPath, string outputVideoPath)
        {
            var args =
                $"-i \"{videoPath}\" -i \"{audioPath}\" " +
                $"-c:v copy -map 0:v:0 -map 1:a:0 -shortest \"{outputVideoPath}\"";

            await RunFfmpegAsync(args);
        }

        public async Task MergeAudioWithVideoAsync(string videoPath, string audioPath, string outputVideoPath)
        {
            var args =
                $"-i \"{videoPath}\" -i \"{audioPath}\" " +
                $"-c:v copy -c:a aac -shortest \"{outputVideoPath}\"";

            await RunFfmpegAsync(args);
        }

        public async Task ExtractAudioFromVideoAsync(string videoPath, string outputAudioPath)
        {
            var args =
                $"-i \"{videoPath}\" -vn -acodec aac \"{outputAudioPath}\"";

            await RunFfmpegAsync(args);
        }

        public async Task CutVideoAsync(string inputPath, string startTime, string endTime, string outputPath)
        {
            var args =
                $"-i \"{inputPath}\" -ss {startTime} -to {endTime} -c copy \"{outputPath}\"";

            await RunFfmpegAsync(args);
        }

        // You can add these later as per your previous methods
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
                        $"-vf scale=1280:720 -c:v libx264 -preset fast -crf 18 " +
                        $"-pix_fmt yuv420p -y \"{segmentPath}\"";

                    await RunFfmpegAsync(ffmpegArgs);

                    videoSegments.Add(segmentPath);
                    index++;
                }

                await File.WriteAllLinesAsync(inputListPath, videoSegments.Select(v => $"file '{v}'"));

                var concatOutputPath = Path.Combine(tempDir, "combined.mp4");

                var concatArgs =
                    $"-f concat -safe 0 -i \"{inputListPath}\" -c copy -y \"{concatOutputPath}\"";

                await RunFfmpegAsync(concatArgs);

                var finalArgs =
                    $"-i \"{concatOutputPath}\" -i \"{audioPath}\" " +
                    $"-c:v copy -c:a aac -shortest -y \"{outputVideoPath}\"";

                await RunFfmpegAsync(finalArgs);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }


        public async Task SplitAndKeepAudioAsync(
    string audioPath,
    List<AudioSegmentDto> segments,
    string outputPath)
        {
            if (segments == null || segments.Count == 0)
                throw new Exception("Segments are required.");

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var listFile = Path.Combine(tempDir, "list.txt");
            var outputSegments = new List<string>();

            try
            {
                for (int i = 0; i < segments.Count; i++)
                {
                    var seg = segments[i];

                    var partPath = Path.Combine(tempDir, $"part_{i}.mp3");

                    // Using -ss and -to (copy mode not reliable for mp3 always)
                    var args =
                        $"-i \"{audioPath}\" -ss {seg.Start} -to {seg.End} " +
                        $"-c:a libmp3lame -y \"{partPath}\"";

                    await RunFfmpegAsync(args);

                    outputSegments.Add(partPath);
                }

                await File.WriteAllLinesAsync(listFile, outputSegments.Select(x => $"file '{x}'"));

                var concatArgs =
                    $"-f concat -safe 0 -i \"{listFile}\" -c copy -y \"{outputPath}\"";

                await RunFfmpegAsync(concatArgs);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        public async Task GenerateTimedImageVideoWithTransitionsAsync(
    List<(string imagePath, int duration, string transition, double transitionDuration)> segments,
    string audioPath,
    string outputVideoPath)
        {
            if (segments == null || segments.Count == 0)
                throw new Exception("At least 1 image segment is required.");

            if (string.IsNullOrWhiteSpace(audioPath))
                throw new Exception("Audio path is required.");

            // 1) Build ffmpeg input arguments
            // Each image becomes a looping video for given duration
            var inputArgs = new List<string>();

            for (int i = 0; i < segments.Count; i++)
            {
                inputArgs.Add($"-loop 1 -t {segments[i].duration} -i \"{segments[i].imagePath}\"");
            }

            // Add audio at the end
            inputArgs.Add($"-i \"{audioPath}\"");

            // 2) Build filter_complex
            var filter = new List<string>();

            // Normalize each image stream: scale, fps, format, reset timestamps
            for (int i = 0; i < segments.Count; i++)
            {
                filter.Add(
                    $"[{i}:v]scale=1280:720,fps=30,format=yuv420p,setpts=PTS-STARTPTS[v{i}]"
                );
            }

            // 3) Build xfade chain
            // Correct offset formula:
            // offset for transition i = sum(previous durations) - sum(previous transition durations)
            // But easiest is incremental tracking

            string lastStream = $"[v0]";
            double offset = segments[0].duration; // end of first segment

            for (int i = 1; i < segments.Count; i++)
            {
                var transition = string.IsNullOrWhiteSpace(segments[i].transition)
                    ? "fade"
                    : segments[i].transition;

                var transDur = segments[i].transitionDuration;

                // Transition must start BEFORE previous ends
                // So transition begins at: (current offset - transDur)
                var transitionStart = offset - transDur;

                var outStream = $"[vxf{i}]";

                filter.Add(
                    $"{lastStream}[v{i}]xfade=transition={transition}:duration={transDur}:offset={transitionStart}{outStream}"
                );

                lastStream = outStream;

                // Update offset for next segment end:
                // We add current segment duration, but overlap transition duration
                offset = transitionStart + segments[i].duration;
            }

            // 4) Build final args
            var args =
                    $"{string.Join(" ", inputArgs)} " +
                    $"-filter_complex \"{string.Join(";", filter)}\" " +
                    $"-map \"{lastStream}\" -map {segments.Count}:a:0 " +
                    $"-c:v libx264 -preset fast -crf 18 " +
                    $"-c:a aac -b:a 192k " +
                    $"-pix_fmt yuv420p -shortest -y \"{outputVideoPath}\"";

            try
            {
                await RunFfmpegAsyncduel(args);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "FFmpeg failed.\n\nArgs:\n" + args + "\n\nError:\n" + ex.Message,
                    ex
                );
            }
        }

        private async Task RunFfmpegAsyncduel(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            string stderr = await process.StandardError.ReadToEndAsync();
            string stdout = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed.\n\nARGS:\n{arguments}\n\nSTDERR:\n{stderr}\n\nSTDOUT:\n{stdout}");
            }
        }


    }
}
