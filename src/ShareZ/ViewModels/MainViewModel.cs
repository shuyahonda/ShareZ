using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareZ.Models;

namespace ShareZ.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private string _recordingDuration = "00:00";

    [RelayCommand]
    private async Task CaptureFullscreen()
    {
        StatusText = "Capturing fullscreen...";
        var path = await App.CaptureService.CaptureFullscreenAsync();
        if (path != null)
        {
            await App.TaskService.ExecuteAfterCaptureAsync(path, CaptureType.Fullscreen);
            StatusText = $"Captured: {Path.GetFileName(path)}";
        }
        else
        {
            StatusText = "Capture failed";
        }
    }

    [RelayCommand]
    private async Task CaptureWindow()
    {
        StatusText = "Capturing active window...";
        var path = await App.CaptureService.CaptureWindowAsync();
        if (path != null)
        {
            await App.TaskService.ExecuteAfterCaptureAsync(path, CaptureType.Window);
            StatusText = $"Captured: {Path.GetFileName(path)}";
        }
        else
        {
            StatusText = "Capture failed";
        }
    }

    [RelayCommand]
    private async Task CaptureRegion()
    {
        StatusText = "Select region to capture...";
        // Pre-capture the screen using GDI BitBlt before showing the overlay.
        // This ensures the overlay displays the actual desktop content instead of a gray screen.
        var screenImagePath = await App.CaptureService.CaptureScreenToTempFileAsync();
        var regionWindow = new Views.RegionCaptureWindow(screenImagePath);
        regionWindow.Activate();
    }

    [RelayCommand]
    private async Task ToggleRecording()
    {
        if (IsRecording)
        {
            var path = await App.RecordingService.StopRecordingAsync();
            IsRecording = false;
            StatusText = path != null ? $"Recording saved: {Path.GetFileName(path)}" : "Recording failed";
            RecordingDuration = "00:00";
        }
        else
        {
            await App.RecordingService.StartRecordingAsync(RecordingFormat.Mp4);
            IsRecording = true;
            StatusText = "Recording...";
            App.RecordingService.RecordingProgress += (s, duration) =>
            {
                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    RecordingDuration = duration.ToString(@"mm\:ss");
                });
            };
        }
    }

    [RelayCommand]
    private async Task ToggleGifRecording()
    {
        if (IsRecording)
        {
            var path = await App.RecordingService.StopRecordingAsync();
            IsRecording = false;
            StatusText = path != null ? $"GIF saved: {Path.GetFileName(path)}" : "Recording failed";
        }
        else
        {
            await App.RecordingService.StartRecordingAsync(RecordingFormat.Gif);
            IsRecording = true;
            StatusText = "Recording GIF...";
        }
    }
}
