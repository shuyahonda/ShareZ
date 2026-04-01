namespace ShareZ.Models;

public class HistoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public string? UploadUrl { get; set; }
    public string? DeletionUrl { get; set; }
    public CaptureType CaptureType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    public string FileName => Path.GetFileName(FilePath);
    public string FileExtension => Path.GetExtension(FilePath);
}
