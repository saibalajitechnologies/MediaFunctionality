using System.Diagnostics;

namespace FunctionalitiesWebAPI.Helper
{
    public class MergeHelper
    {

        public static async Task MergeVideosAsync(string videoFolderPath, List<string> videoFiles)
        {
            string inputListFile = Path.Combine(videoFolderPath, "input.txt");

            // Write file list
            await File.WriteAllLinesAsync(inputListFile, videoFiles.Select(v => $"file '{v}'"));

            string outputFile = Path.Combine(videoFolderPath, "merged.mp4");

            string ffmpegPath = CommonHelper.GetFfmpegExecutable();

            string arguments = $"-f concat -safe 0 -i \"{inputListFile}\" -c:v libx264 -c:a aac -strict experimental -y \"{outputFile}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    WorkingDirectory = videoFolderPath,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string stdErr = await process.StandardError.ReadToEndAsync();
            string stdOut = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed. Exit Code: {process.ExitCode}\nSTDOUT:\n{stdOut}\nSTDERR:\n{stdErr}");
            }

            if (!File.Exists(outputFile))
            {
                throw new FileNotFoundException("FFmpeg did not produce the merged.mp4 output.", outputFile);
            }
        }

    }
}
