using FunctionalitiesWebAPI.DTO;
using System.Diagnostics;
using System.Text;

namespace FunctionalitiesWebAPI.Helper
{
    public class VideoGenerator
    {

        public void SplitAndKeepAudio(string inputAudio, List<AudioSegmentDto> segments, string outputAudio)
        {
            var ffmpegPath = CommonHelper.GetFfmpegExecutable();
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            var segmentFiles = new List<string>();

            // Create each segment
            int index = 0;
            foreach (var segment in segments)
            {
                var segmentFile = Path.Combine(tempFolder, $"segment_{index}.mp3");
                var duration = segment.End - segment.Start;
                var args = $"-y -i \"{inputAudio}\" -ss {segment.Start} -t {duration} -acodec copy \"{segmentFile}\"";
                RunFfmpegCommandsplit(ffmpegPath, args);
                segmentFiles.Add(segmentFile);
                index++;
            }

            // Create concat list file
            var listFile = Path.Combine(tempFolder, "concat.txt");
            File.WriteAllLines(listFile, segmentFiles.Select(f => $"file '{f.Replace("\\", "/")}'"));

            // Merge selected segments back into one audio file
            var concatArgs = $"-y -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputAudio}\"";
            RunFfmpegCommandsplit(ffmpegPath, concatArgs);

            Directory.Delete(tempFolder, true);
        }

        private void RunFfmpegCommandsplit(string ffmpegPath, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("FFmpeg failed: " + error);
        }


        public void MergeAudioWithVideo(string videoPath, string audioPath, string outputPath)
        {
            var ffmpegPath = CommonHelper.GetFfmpegExecutable();
            // FFmpeg command: replace the video’s existing audio track with the given one
            var arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -map 0:v:0 -map 1:a:0 -shortest -y \"{outputPath}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new Exception($"FFmpeg failed: {output}");
            }
        }

        public void GenerateVideo(string imagePath, string audioPath, string outputPath)
        {
            var ffmpegPath = CommonHelper.GetFfmpegExecutable();

            var args = $"-y -loop 1 -i \"{imagePath}\" -i \"{audioPath}\" -c:v libx264 -c:a aac -b:a 192k -shortest -pix_fmt yuv420p \"{outputPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Read only error output (ffmpeg writes everything here)
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception("FFmpeg failed: " + error);
            }
        }

        public void GenerateVideoFromAudio(string videoPath, string audioPath, string outputPath)
        {
            var ffmpegPath = CommonHelper.GetFfmpegExecutable();
            var ffprobePath = CommonHelper.GetFfprobeExecutable();

            // Get video duration
            var videoDuration = GetMediaDuration(ffprobePath, videoPath);
            var audioDuration = GetMediaDuration(ffprobePath, audioPath);

            var extensionDuration = audioDuration - videoDuration;

            string args;

            if (extensionDuration > 0)
            {
                // Extend video using tpad (freeze last frame)
                args = $"-y -i \"{videoPath}\" -i \"{audioPath}\" " +
                       $"-filter_complex \"[0:v]tpad=stop_mode=clone:stop_duration={extensionDuration:F2}[v]\" " +
                       "-map \"[v]\" -map 1:a -c:v libx264 -c:a aac -shortest -pix_fmt yuv420p " +
                       $"\"{outputPath}\"";
            }
            else
            {
                // Audio is shorter or equal, no need to pad
                args = $"-y -i \"{videoPath}\" -i \"{audioPath}\" -map 0:v:0 -map 1:a:0 -c:v libx264 -c:a aac -shortest -pix_fmt yuv420p " +
                       $"\"{outputPath}\"";
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception("FFmpeg failed: " + error);
            }
        }

        private double GetMediaDuration(string ffprobePath, string filePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return double.TryParse(output.Trim(), out double duration) ? duration : 0;
        }

        public void GenerateTimedImageVideo(List<(string imagePath, int duration)> imageSegments, string audioPath, string outputPath)
        {
            var ffmpegPath = CommonHelper.GetFfmpegExecutable();
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            var videoListFile = Path.Combine(tempFolder, "file-list.txt");
            var sb = new StringBuilder();

            int index = 0;
            foreach (var segment in imageSegments)
            {
                var segmentPath = Path.Combine(tempFolder, $"segment{index}.mp4");
                var args = $"-loop 1 -t {segment.duration} -i \"{segment.imagePath}\" " +
                           $"-vf \"scale=1280:720\" -c:v libx264 -pix_fmt yuv420p -r 25 \"{segmentPath}\"";

                RunFfmpegCommand(ffmpegPath, args);
                sb.AppendLine($"file '{segmentPath.Replace("\\", "/")}'");
                index++;
            }

            var listFilePath = Path.Combine(tempFolder, "file-list.txt");
            File.WriteAllText(listFilePath, sb.ToString());

            var mergedVideoPath = Path.Combine(tempFolder, "merged_video.mp4");
            RunFfmpegCommand(ffmpegPath, $"-f concat -safe 0 -i \"{listFilePath}\" -c copy \"{mergedVideoPath}\"");

            RunFfmpegCommand(ffmpegPath, $"-i \"{mergedVideoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -shortest \"{outputPath}\"");

            Directory.Delete(tempFolder, true);
        }

        private void RunFfmpegCommand(string ffmpegPath, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("FFmpeg failed: " + error);
        }

    }
}
