using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xabe.FFmpeg;
using static System.Net.Mime.MediaTypeNames;

namespace FunctionalitiesWebAPI.Helper
{
    public static class MediaCompress
    {

        public static async Task CompressMediaVideo(string inputPath, string outputPath)
        {
            var ffmpegPath = CommonHelper.GetFfmpegExecutable();
            var errorOutput = new StringBuilder();

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{inputPath}\" " +
                                "-c:v libx264 -preset slow -crf 30 " +
                                "-maxrate 800k -bufsize 1600k " +
                                "-c:a aac -b:a 96k -movflags +faststart " +
                                $"\"{outputPath}\"",
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    errorOutput.AppendLine(e.Data);
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"FFmpeg error:\n{errorOutput}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compression failed: {ex.Message}");
                throw;
            }
        }


        //private static string GetFFMpeg()
        //{
        //    string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe");
        //    if (!File.Exists(ffmpegPath))
        //        throw new FileNotFoundException("FFmpeg executable not found at: " + ffmpegPath);

        //    return ffmpegPath;
        //}

        //public static async Task CompressVideo(string inputPath, string outputPath)
        //{
        //    var ffmpegPath = CommonHelper.GetFfmpeg(); // Ensure path is valid
        //    var errorOutput = new StringBuilder();

        //    var process = new Process
        //    {
        //        StartInfo = new ProcessStartInfo
        //        {
        //            FileName = ffmpegPath,
        //            //Arguments = $"-hwaccel auto -i \"{inputPath}\" -c:v h264_nvenc -preset fast -cq 30 -c:a copy -movflags +faststart \"{outputPath}\"",
        //            //Arguments = $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 23 -c:a copy -movflags +faststart \"{outputPath}\"",
        //            Arguments = $"-i \"{inputPath}\" " +
        //    "-c:v libx264 " +
        //    "-preset slow " +
        //    "-crf 30 " +
        //    "-maxrate 800k -bufsize 1600k " +
        //    "-c:a aac -b:a 96k " +
        //    "-movflags +faststart " +
        //    $"\"{outputPath}\"",
        //            RedirectStandardOutput = false,
        //            RedirectStandardError = true,
        //            UseShellExecute = false,
        //            CreateNoWindow = true
        //        },
        //        EnableRaisingEvents = true
        //    };

        //    process.ErrorDataReceived += (sender, e) =>
        //    {
        //        if (!string.IsNullOrWhiteSpace(e.Data))
        //            errorOutput.AppendLine(e.Data);
        //    };

        //    try
        //    {
        //        process.Start();
        //        process.BeginErrorReadLine();
        //        await process.WaitForExitAsync();

        //        if (process.ExitCode != 0)
        //            throw new Exception($"FFmpeg error: {errorOutput}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Compression failed: {ex.Message}");
        //        throw;
        //    }
        //}

        public static async Task<string> CompressAudio(string inputPath)
        {
            //string ffmpegPath = GetFFMpeg();
            string ffmpegFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");

            FFmpeg.SetExecutablesPath(ffmpegFolder);

            string directory = Path.GetDirectoryName(inputPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Invalid input path");

            string outputFileName = Path.GetFileNameWithoutExtension(inputPath) + "_compressed.mp3";
            string outputPath = Path.Combine(directory, outputFileName);

            //int targetBitrateKbps = 96;

            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{inputPath}\" -b:a 64k -ac 1 \"{outputPath}\"", ParameterPosition.PreInput);
            //.AddParameter($"-i \"{inputPath}\" -b:a {targetBitrateKbps}k \"{outputPath}\"", ParameterPosition.PreInput);

            await conversion.Start();
            return outputPath;
        }


        public static Task<byte[]> CompressImage(Stream originalStream, int quality = 50)
        {
            using var inputStream = new SKManagedStream(originalStream);
            using var original = SKBitmap.Decode(inputStream);

            using var image = SKImage.FromBitmap(original);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            return Task.FromResult(data.ToArray());
        }

    }
}
