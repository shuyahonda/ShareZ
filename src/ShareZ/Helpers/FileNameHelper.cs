using ShareZ.Models;

namespace ShareZ.Helpers;

public static class FileNameHelper
{
    public static string GenerateFilePath(string folder, string pattern, ImageFormat format)
    {
        var now = DateTime.Now;
        var fileName = pattern
            .Replace("%Y", now.ToString("yyyy"))
            .Replace("%M", now.ToString("MM"))
            .Replace("%D", now.ToString("dd"))
            .Replace("%h", now.ToString("HH"))
            .Replace("%m", now.ToString("mm"))
            .Replace("%s", now.ToString("ss"))
            .Replace("%ms", now.ToString("fff"))
            .Replace("%n", Guid.NewGuid().ToString("N")[..8]);

        var extension = format switch
        {
            ImageFormat.Png => ".png",
            ImageFormat.Jpg => ".jpg",
            ImageFormat.Bmp => ".bmp",
            ImageFormat.Gif => ".gif",
            ImageFormat.Tiff => ".tiff",
            ImageFormat.WebP => ".webp",
            _ => ".png"
        };

        var filePath = Path.Combine(folder, fileName + extension);

        // Ensure unique filename
        int counter = 1;
        while (File.Exists(filePath))
        {
            filePath = Path.Combine(folder, $"{fileName}_{counter}{extension}");
            counter++;
        }

        return filePath;
    }

    public static string GenerateRecordingFilePath(string folder, RecordingFormat format)
    {
        var now = DateTime.Now;
        var fileName = $"ShareZ_Recording_{now:yyyy-MM-dd_HH-mm-ss}";

        var extension = format switch
        {
            RecordingFormat.Mp4 => ".mp4",
            RecordingFormat.Gif => ".gif",
            RecordingFormat.Webm => ".webm",
            _ => ".mp4"
        };

        return Path.Combine(folder, fileName + extension);
    }
}
