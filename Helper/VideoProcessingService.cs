using System.Diagnostics;

namespace FunctionalitiesWebAPI.Helper;

public class VideoProcessingService
{
    private readonly string _ffmpegPath;

    public VideoProcessingService()
    {
        var ffmpegFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        _ffmpegPath = Path.Combine(ffmpegFolder, "ffmpeg.exe");
    }

    public async Task<string> CreateFullYoutubeVideo(string mainVideoPath, int episodeNumber)
    {
        var tempMerged = Path.Combine("wwwroot/videos", $"{Guid.NewGuid()}_merged.mp4");
        var finalOutput = Path.Combine("wwwroot/videos", $"{Guid.NewGuid()}_final.mp4");

        await MergeIntroMainOutro(mainVideoPath, tempMerged);
        await ApplyBrandingAndAnimations(tempMerged, finalOutput, episodeNumber);

        return finalOutput;
    }

    private async Task MergeIntroMainOutro(string mainVideoPath, string outputPath)
    {
        var intro = "assets/intro.mp4";
        var outro = "assets/outro.mp4";
        var listFile = Path.Combine("wwwroot/videos", $"{Guid.NewGuid()}_list.txt");

        await File.WriteAllTextAsync(listFile,
            $"file '{Path.GetFullPath(intro)}'{Environment.NewLine}" +
            $"file '{Path.GetFullPath(mainVideoPath)}'{Environment.NewLine}" +
            $"file '{Path.GetFullPath(outro)}'");

        var args = $"-f concat -safe 0 -i \"{listFile}\" -c:v libx264 -preset fast -crf 23 -c:a aac -b:a 128k \"{outputPath}\"";

        await RunFfmpeg(args);
    }

    private async Task ApplyBrandingAndAnimations(string inputPath, string outputPath, int episodeNumber)
    {
        var logo = "assets/logo.png";
        var subscribe = "assets/subscribe.png";

        var filter = $@"
overlay=W-w-20:20,
drawtext=text='Episode {episodeNumber}':fontcolor=yellow:fontsize=40:x=20:y=H-80,
drawtext=text='Om Sai Ram':fontcolor=white:fontsize=60:
x=(w-text_w)/2:y=h-150:
alpha='if(lt(t,5),0,if(lt(t,7),(t-5)/2,if(lt(t,10),1,if(lt(t,12),(12-t)/2,0))))',
overlay=(main_w-overlay_w)/2:H-150:enable='between(t,10,15)'
";

        var args =
            $"-i \"{inputPath}\" -i \"{logo}\" -i \"{subscribe}\" " +
            $"-filter_complex \"{filter}\" " +
            "-c:v libx264 -preset fast -crf 23 -c:a aac -b:a 128k " +
            $"\"{outputPath}\"";

        await RunFfmpeg(args);
    }

    private async Task RunFfmpeg(string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"FFmpeg failed: {error}");
        }
    }
}