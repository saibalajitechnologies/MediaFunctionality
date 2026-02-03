namespace FunctionalitiesWebAPI.Helper
{
    public static class CommonHelper
    {
        private static readonly string ffmpegFolder = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        private static readonly string ffmpegExe = Path.Combine(ffmpegFolder, "ffmpeg.exe");
        private static readonly string ffprobeExe = Path.Combine(ffmpegFolder, "ffprobe.exe");

        public static string GetFfmpegExecutable()//GetFfmpeg()
        {
            if (!File.Exists(ffmpegExe))
                throw new FileNotFoundException("FFmpeg executable not found.", ffmpegExe);

            return ffmpegExe;
        }

        public static string GetFfprobeExecutable()
        {
            if (!File.Exists(ffprobeExe))
                throw new FileNotFoundException("FFmpeg executable not found.", ffmpegExe);

            return ffprobeExe;
        }


        public static string GetFfmpegFolder()//GetFfmpegFolder()
        {
            if (!Directory.Exists(ffmpegFolder))
                throw new DirectoryNotFoundException($"FFmpeg folder not found: {ffmpegFolder}");

            return ffmpegFolder;
        }
    }
}
