using System.Globalization;
using System.Text.RegularExpressions;

namespace FunctionalitiesWebAPI.Helper
{
    public class EmailSplitter
    {
        public static async Task readingfiles()
        {
            string inputFilePath = @"D:\Videos\Email Proof\Gmail - 50 - 50.pdf"; // Input file path
            string outputDirectory = @"D:\Videos\SplitEmails\"; // Output directory

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string content = await File.ReadAllTextAsync(inputFilePath);

            // Split by "---------- Forwarded message ----------"
            string[] emailParts = Regex.Split(content, "---------- Forwarded message ----------");

            foreach (var part in emailParts)
            {
                string trimmedPart = part.Trim();
                if (string.IsNullOrWhiteSpace(trimmedPart)) continue;

                // Extract date line
                Match dateMatch = Regex.Match(trimmedPart, @"Date:\s*(.*)", RegexOptions.IgnoreCase);
                if (dateMatch.Success)
                {
                    string dateLine = dateMatch.Groups[1].Value.Trim();
                    try
                    {
                        DateTime parsedDate = DateTime.Parse(dateLine, null, DateTimeStyles.AdjustToUniversal);
                        string fileName = $"{parsedDate:yyyy_MMMM_dd_HHmm}.txt";
                        string fullPath = Path.Combine(outputDirectory, fileName);
                        File.WriteAllText(fullPath, trimmedPart);
                    }
                    catch (FormatException)
                    {
                        // If parsing fails, fallback to default numbered file
                        string fallbackFile = Path.Combine(outputDirectory, $"unknown_date_{Guid.NewGuid().ToString("N").Substring(0, 6)}.txt");
                        File.WriteAllText(fallbackFile, trimmedPart);
                    }
                }
                else
                {
                    // If no Date line is found, fallback to default numbered file
                    string fallbackFile = Path.Combine(outputDirectory, $"no_date_{Guid.NewGuid().ToString("N").Substring(0, 6)}.txt");
                    File.WriteAllText(fallbackFile, trimmedPart);
                }
            }
        }
    }
}
