using SkiaSharp;
using System.Diagnostics;
using System.Text;
using Xabe.FFmpeg;

namespace FunctionalitiesWebAPI.Helper
{
    public class MediaManipulationHelper
    {
        private static async Task RunFFmpegProcess(string ffmpegPath, string arguments, string workingDir)
        {
            var errorOutput = new StringBuilder();

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var stdError = await process.StandardError.ReadToEndAsync();
            var stdOutput = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg Failed\nExit Code: {process.ExitCode}\nSTDOUT:\n{stdOutput}\nSTDERR:\n{stdError}");
            }
        }

        public static async Task CompressMediaVideo(string inputPath, string outputPath)
        {
            string ffmpegPath = CommonHelper.GetFfmpegExecutable();

            string args = $"-i \"{inputPath}\" -c:v libx264 -preset slow -crf 30 " +
                          "-maxrate 800k -bufsize 1600k -c:a aac -b:a 96k -movflags +faststart " +
                          $"\"{outputPath}\"";

            await RunFFmpegProcess(ffmpegPath, args, Path.GetDirectoryName(inputPath)!);
        }

        public static async Task HighMediaVideo(string inputPath, string outputPath)
        {
            string ffmpegPath = CommonHelper.GetFfmpegExecutable();

            var args =
                $"-i \"{inputPath}\" " +
                "-vf \"scale=1920:1080:flags=lanczos,unsharp=5:5:1.0\" " +
                "-c:v libx264 " +
                "-preset slow " +
                "-crf 18 " +
                "-c:a aac -b:a 192k " +
                "-movflags +faststart " +
                $"\"{outputPath}\"";

            await RunFFmpegProcess(ffmpegPath, args, Path.GetDirectoryName(inputPath)!);
        }

        public static async Task<string> CompressAudio(string inputPath)
        {
            string ffmpegFolder = CommonHelper.GetFfmpegFolder();
            FFmpeg.SetExecutablesPath(ffmpegFolder);

            string? directory = Path.GetDirectoryName(inputPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Invalid input path.");

            string outputFile = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(inputPath)}_compressed.mp3");

            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{inputPath}\" -b:a 64k -ac 1 \"{outputFile}\"", ParameterPosition.PreInput);

            await conversion.Start();
            return outputFile;
        }

        public static Task<byte[]> CompressImage(Stream originalStream, int quality = 50)
        {
            using var inputStream = new SKManagedStream(originalStream);
            using var bitmap = SKBitmap.Decode(inputStream);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            return Task.FromResult(data.ToArray());
        }

        public static async Task MergeVideosAsync(string videoFolderPath, List<string> fileNames)
        {
            string inputListPath = Path.Combine(videoFolderPath, "input.txt");
            string outputPath = Path.Combine(videoFolderPath, "merged.mp4");

            var listLines = fileNames.Select(name => $"file '{name.Replace("'", "'\\''")}'");
            await File.WriteAllLinesAsync(inputListPath, listLines);

            string ffmpegPath = CommonHelper.GetFfmpegExecutable();
            string args = $"-f concat -safe 0 -i \"{inputListPath}\" -c:v libx264 -c:a aac -strict experimental -y \"{outputPath}\"";

            await RunFFmpegProcess(ffmpegPath, args, videoFolderPath);

            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Merged output not found.", outputPath);
        }

        public static async Task<string> MergeVideo(string videoFolderPath, List<string> fullFilePaths)
        {
            if (fullFilePaths == null || fullFilePaths.Count < 2)
                throw new ArgumentException("At least two videos are required for merging.");

            string outputPath = Path.Combine(videoFolderPath, "merged.mp4");

            string ffmpegPath = CommonHelper.GetFfmpegExecutable();

            string input1 = fullFilePaths[0];
            string input2 = fullFilePaths[1];

            string args =
                $"-i \"{input1}\" -i \"{input2}\" " +
                "-filter_complex \"[0:v:0][0:a:0][1:v:0][1:a:0]concat=n=2:v=1:a=1[outv][outa]\" " +
                "-map \"[outv]\" -map \"[outa]\" " +
                "-c:v libx264 -preset veryfast -crf 23 " +
                "-c:a aac -b:a 128k " +
                "-movflags +faststart " +
                "-y " +
                $"\"{outputPath}\"";

            await RunFFmpegProcessForVideo(ffmpegPath, args, videoFolderPath);

            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Merged output not found.", outputPath);

            return outputPath;
        }

        public static async Task<string> MergeVideoSame(string videoFolderPath, List<string> fullFilePaths)
        {
            string inputListPath = Path.Combine(videoFolderPath, "input.txt");
            string outputPath = Path.Combine(videoFolderPath, "merged.mp4");

            var listLines = fullFilePaths.Select(path =>
            {
                string safePath = path.Replace("'", "'\\''");
                return $"file '{safePath}'";
            });

            await File.WriteAllLinesAsync(inputListPath, listLines);

            string ffmpegPath = CommonHelper.GetFfmpegExecutable();

            string args =
                $"-f concat -safe 0 -i \"{inputListPath}\" " +
                "-fflags +genpts " +
                "-c:v libx264 -preset veryfast -crf 23 " +
                "-c:a aac -b:a 128k " +
                "-movflags +faststart " +
                "-y " +
                $"\"{outputPath}\"";

            await RunFFmpegProcessForVideo(ffmpegPath, args, videoFolderPath);

            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Merged output not found.", outputPath);

            return outputPath;
        }


        private static async Task RunFFmpegProcessForVideo(string ffmpegPath, string arguments, string workingDir)
        {
            var stdError = new StringBuilder();
            var stdOutput = new StringBuilder();

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception(
                    $"FFmpeg Failed\n" +
                    $"Exit Code: {process.ExitCode}\n\n" +
                    $"Arguments:\n{arguments}\n\n" +
                    $"STDOUT:\n{outputTask.Result}\n\n" +
                    $"STDERR:\n{errorTask.Result}"
                );
            }
        }

        public static async Task<string> ConvertMpegToMp3(string inputPath)
        {
            if (!System.IO.File.Exists(inputPath))
                throw new FileNotFoundException("Input file not found.");

            var outputPath = Path.ChangeExtension(inputPath, ".mp3");

            //var ffmpegPath = "ffmpeg"; // or full path to ffmpeg.exe
            var ffmpegFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");

            var arguments = $"-i \"{inputPath}\" -vn -ar 44100 -ac 2 -b:a 192k \"{outputPath}\" -y";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegFolder,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                    throw new Exception("FFmpeg Error: " + error);
            }

            return outputPath;
        }
    }
}