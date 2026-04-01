using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareZ.Models;
using ShareZ.Services;
using System.Collections.ObjectModel;

namespace ShareZ.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // General
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _showTrayIcon;
    [ObservableProperty] private bool _playSoundAfterCapture;
    [ObservableProperty] private bool _showNotificationAfterCapture;

    // Capture
    [ObservableProperty] private string _screenshotFolder = string.Empty;
    [ObservableProperty] private string _screenshotFilePattern = string.Empty;
    [ObservableProperty] private int _selectedImageFormatIndex;
    [ObservableProperty] private int _jpegQuality;
    [ObservableProperty] private bool _captureCursor;

    // After Capture
    [ObservableProperty] private bool _copyImageToClipboard;
    [ObservableProperty] private bool _saveToFile;
    [ObservableProperty] private bool _openInEditor;
    [ObservableProperty] private bool _uploadAfterCapture;
    [ObservableProperty] private bool _showNotification;

    // Recording
    [ObservableProperty] private string _recordingFolder = string.Empty;
    [ObservableProperty] private int _selectedRecordingFormatIndex;
    [ObservableProperty] private int _videoFps;
    [ObservableProperty] private int _videoBitrate;
    [ObservableProperty] private int _gifFps;
    [ObservableProperty] private bool _recordAudio;

    // Hotkeys
    public ObservableCollection<HotkeyBinding> Hotkeys { get; } = new();

    // Upload
    public ObservableCollection<UploadDestination> UploadDestinations { get; } = new();

    public void LoadSettings()
    {
        var s = App.Settings;

        StartWithWindows = s.StartWithWindows;
        MinimizeToTray = s.MinimizeToTray;
        ShowTrayIcon = s.ShowTrayIcon;
        PlaySoundAfterCapture = s.PlaySoundAfterCapture;
        ShowNotificationAfterCapture = s.ShowNotificationAfterCapture;

        ScreenshotFolder = s.ScreenshotFolder;
        ScreenshotFilePattern = s.ScreenshotFilePattern;
        SelectedImageFormatIndex = (int)s.DefaultImageFormat;
        JpegQuality = s.JpegQuality;
        CaptureCursor = s.CaptureCursor;

        CopyImageToClipboard = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.CopyImageToClipboard);
        SaveToFile = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.SaveToFile);
        OpenInEditor = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.OpenInEditor);
        UploadAfterCapture = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.Upload);
        ShowNotification = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.ShowNotification);

        RecordingFolder = s.RecordingFolder;
        SelectedRecordingFormatIndex = (int)s.DefaultRecordingFormat;
        VideoFps = s.VideoFps;
        VideoBitrate = s.VideoBitrate;
        GifFps = s.GifFps;
        RecordAudio = s.RecordAudio;

        Hotkeys.Clear();
        foreach (var hk in s.Hotkeys) Hotkeys.Add(hk);

        UploadDestinations.Clear();
        foreach (var ud in s.UploadDestinations) UploadDestinations.Add(ud);
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        var s = App.Settings;

        s.StartWithWindows = StartWithWindows;
        s.MinimizeToTray = MinimizeToTray;
        s.ShowTrayIcon = ShowTrayIcon;
        s.PlaySoundAfterCapture = PlaySoundAfterCapture;
        s.ShowNotificationAfterCapture = ShowNotificationAfterCapture;

        s.ScreenshotFolder = ScreenshotFolder;
        s.ScreenshotFilePattern = ScreenshotFilePattern;
        s.DefaultImageFormat = (ImageFormat)SelectedImageFormatIndex;
        s.JpegQuality = JpegQuality;
        s.CaptureCursor = CaptureCursor;

        var tasks = AfterCaptureTask.None;
        if (CopyImageToClipboard) tasks |= AfterCaptureTask.CopyImageToClipboard;
        if (SaveToFile) tasks |= AfterCaptureTask.SaveToFile;
        if (OpenInEditor) tasks |= AfterCaptureTask.OpenInEditor;
        if (UploadAfterCapture) tasks |= AfterCaptureTask.Upload;
        if (ShowNotification) tasks |= AfterCaptureTask.ShowNotification;
        s.AfterCaptureTasks = tasks;

        s.RecordingFolder = RecordingFolder;
        s.DefaultRecordingFormat = (RecordingFormat)SelectedRecordingFormatIndex;
        s.VideoFps = VideoFps;
        s.VideoBitrate = VideoBitrate;
        s.GifFps = GifFps;
        s.RecordAudio = RecordAudio;

        s.Hotkeys = Hotkeys.ToList();
        s.UploadDestinations = UploadDestinations.ToList();

        await SettingsService.SaveAsync(s);
    }

    [RelayCommand]
    private async Task BrowseScreenshotFolder()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            ScreenshotFolder = folder.Path;
        }
    }

    [RelayCommand]
    private async Task BrowseRecordingFolder()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            RecordingFolder = folder.Path;
        }
    }
}
