using ShareZ.Models;
using ShareZ.Views.Pages;

namespace ShareZ.Services;

public class TaskService
{
    private readonly AppSettings _settings;

    /// Raised when the editor should open with a file path.
    /// MainWindow subscribes to this to navigate the content frame.
    public event EventHandler<string>? OpenInEditorRequested;

    public TaskService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task ExecuteAfterCaptureAsync(string filePath, CaptureType captureType)
    {
        var tasks = _settings.AfterCaptureTasks;

        if (tasks.HasFlag(AfterCaptureTask.CopyImageToClipboard))
        {
            App.ClipboardService.CopyImage(filePath);
        }

        if (tasks.HasFlag(AfterCaptureTask.CopyFilePath))
        {
            App.ClipboardService.CopyFilePath(filePath);
        }

        if (tasks.HasFlag(AfterCaptureTask.Upload))
        {
            var result = await App.UploadService.UploadAsync(filePath);
            if (result != null)
            {
                if (tasks.HasFlag(AfterCaptureTask.CopyUrl) && !string.IsNullOrEmpty(result.Url))
                {
                    App.ClipboardService.CopyUrl(result.Url);
                }
            }
        }

        if (tasks.HasFlag(AfterCaptureTask.OpenInEditor))
        {
            OpenInEditorRequested?.Invoke(this, filePath);
        }

        if (tasks.HasFlag(AfterCaptureTask.ShowNotification))
        {
            ShowNotification("Screenshot captured", Path.GetFileName(filePath));
        }

        // Add to history
        var fileInfo = new FileInfo(filePath);
        var historyItem = new HistoryItem
        {
            FilePath = filePath,
            CaptureType = captureType,
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            Timestamp = DateTime.Now
        };
        await App.HistoryService.AddAsync(historyItem);
    }

    private void ShowNotification(string title, string message)
    {
        if (App.TrayIconService != null)
        {
            App.TrayIconService.ShowBalloonNotification(title, message);
        }
        System.Diagnostics.Debug.WriteLine($"Notification: {title} - {message}");
    }
}
