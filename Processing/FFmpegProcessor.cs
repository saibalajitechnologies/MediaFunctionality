using FunctionalitiesWebAPI.Services.Interfaces;
using System.Diagnostics;

namespace FunctionalitiesWebAPI.Processing
{
    public class FFmpegProcessor : IFFmpegProcessor
    {
        private static readonly string ffmpegFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        private static readonly string ffmpegExe = Path.Combine(ffmpegFolder, "ffmpeg.exe");
        private static readonly string ffprobeExe = Path.Combine(ffmpegFolder, "ffprobe.exe");


        public async Task<string> RunCommand(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegExe,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output;
        }

        public async Task<string> RunCommandWithOutput(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffprobeExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();
            return output;
        }


    }
}
