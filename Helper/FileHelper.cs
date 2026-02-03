using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace FunctionalitiesWebAPI.Helper
{
    public static class FileHelper
    {

        public static async Task readingfiles()
        {
            string inputFilePath = @"D:\Videos\sai.txt"; // Input file path
            string outputDirectory = @"D:\Videos\SplitEmails\";      // Output directory

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string content = await File.ReadAllTextAsync(inputFilePath);

            // Split by "On ... wrote:" while keeping that line
            string pattern = @"(?=On .*? wrote:)";
            string[] emailParts = Regex.Split(content, pattern, RegexOptions.Multiline);

            int count = 1;
            foreach (var part in emailParts)
            {
                string emailFilePath = Path.Combine(outputDirectory, $"email_{count++}.txt");
                File.WriteAllText(emailFilePath, part.Trim());
            }

            //Console.WriteLine("Emails successfully split and saved.");
        }
        //public static IActionResult ValidateFile(IFormFile file, long maxSizeBytes, string label = "File", string[] allowedMimeTypes = null)
        //{
        //    if (allowedMimeTypes != null && !allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))            
        //        return new BadRequestObjectResult($"{label} must be one of the following types: {string.Join(", ", allowedMimeTypes)}");

        //    if (file == null || file.Length == 0)
        //        return new BadRequestObjectResult($"{label} is required.");

        //    if (file.Length > maxSizeBytes)
        //        return new BadRequestObjectResult($"{label} exceeds allowed size ({maxSizeBytes / (1024 * 1024)} MB).");

        //    return null;
        //}

        public static IActionResult ValidateFile(IFormFile file, long maxSizeBytes, string label = "File", string[] allowedMimeTypes = null)
        {
            List<string> errors = new();

            if (file == null || file.Length == 0)
                errors.Add($"{label} is required.");
            else
            {
                if (file.Length > maxSizeBytes)
                    errors.Add($"{label} exceeds allowed size ({maxSizeBytes / (1024 * 1024)} MB).");

                if (allowedMimeTypes != null && !allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                    errors.Add($"{label} must be one of: {string.Join(", ", allowedMimeTypes)}");
            }

            if (errors.Any())
                return new BadRequestObjectResult(new { errors });

            return null;
        }


        public static IActionResult ValidateFileCount(List<IFormFile> files, int minCount, string label)
        {
            if (files == null || files.Count < minCount)
                return new BadRequestObjectResult($"Please upload at least {minCount} {label.ToLower()}.");

            return null;
        }


        public static bool IsValidFile(IFormFile file, long maxSizeInBytes)
        {
            return file != null && file.Length > 0 && file.Length <= maxSizeInBytes;
        }

        public static string GenerateTempPath(string suffixOrExtension = "")
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + suffixOrExtension);
        }

        public static async Task SaveFileAsync(IFormFile file, string path)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        public static async Task<byte[]> ReadFileAsBytesAsync(string path)
        {
            return await File.ReadAllBytesAsync(path);
        }

        public static void SafeDelete(string path, int retries = 5, int delayMs = 200)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        return;
                    }
                }
                catch (IOException) { Thread.Sleep(delayMs); }
                catch (UnauthorizedAccessException) { Thread.Sleep(delayMs); }
            }
        }
    }
}
