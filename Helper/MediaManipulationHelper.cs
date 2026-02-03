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

        public static async Task<string> CompressAudio(string inputPath)
        {
            string ffmpegFolder = CommonHelper.GetFfmpegFolder();
            FFmpeg.SetExecutablesPath(ffmpegFolder);

            string directory = Path.GetDirectoryName(inputPath);
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
    }
}