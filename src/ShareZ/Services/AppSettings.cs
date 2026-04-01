using ShareZ.Models;
using Windows.System;

namespace ShareZ.Services;

public class AppSettings
{
    // General
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowTrayIcon { get; set; } = true;
    public bool PlaySoundAfterCapture { get; set; } = true;
    public bool ShowNotificationAfterCapture { get; set; } = true;

    // Capture
    public string ScreenshotFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ShareZ");
    public string ScreenshotFilePattern { get; set; } = "ShareZ_%Y-%M-%D_%h-%m-%s";
    public ImageFormat DefaultImageFormat { get; set; } = ImageFormat.Png;
    public int JpegQuality { get; set; } = 90;
    public bool CaptureCursor { get; set; } = true;
    public int RegionCaptureDelay { get; set; }

    // After Capture Tasks (flags)
    public AfterCaptureTask AfterCaptureTasks { get; set; } =
        AfterCaptureTask.CopyImageToClipboard |
        AfterCaptureTask.SaveToFile |
        AfterCaptureTask.ShowNotification;

    // Recording
    public string RecordingFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ShareZ");
    public RecordingFormat DefaultRecordingFormat { get; set; } = RecordingFormat.Mp4;
    public int VideoFps { get; set; } = 30;
    public int VideoBitrate { get; set; } = 8000;
    public int GifFps { get; set; } = 15;
    public bool RecordAudio { get; set; }

    // Upload
    public List<UploadDestination> UploadDestinations { get; set; } = new()
    {
        new UploadDestination
        {
            Name = "Imgur",
            Type = "Imgur",
            ApiUrl = "https://api.imgur.com/3/image",
            ApiKey = "",
            IsDefault = true
        }
    };

    // Hotkeys
    public List<HotkeyBinding> Hotkeys { get; set; } = GetDefaultHotkeys();

    // Auto Capture
    public bool AutoCaptureEnabled { get; set; }
    public int AutoCaptureInterval { get; set; } = 5000; // ms
    public CaptureType AutoCaptureType { get; set; } = CaptureType.Fullscreen;

    // Scrolling Capture
    public int ScrollDelay { get; set; } = 500;
    public int MaxScrollCount { get; set; } = 20;

    // Tools
    public bool ColorPickerShowPreview { get; set; } = true;
    public string ColorPickerFormat { get; set; } = "HEX";

    private static List<HotkeyBinding> GetDefaultHotkeys() => new()
    {
        new HotkeyBinding
        {
            Name = "Capture Region",
            Action = "CaptureRegion",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.Number4
        },
        new HotkeyBinding
        {
            Name = "Capture Fullscreen",
            Action = "CaptureFullscreen",
            Modifiers = VirtualKeyModifiers.None,
            Key = VirtualKey.Snapshot
        },
        new HotkeyBinding
        {
            Name = "Capture Window",
            Action = "CaptureWindow",
            Modifiers = VirtualKeyModifiers.Menu,
            Key = VirtualKey.Snapshot
        },
        new HotkeyBinding
        {
            Name = "Start/Stop Recording",
            Action = "ToggleRecording",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.Number5
        },
        new HotkeyBinding
        {
            Name = "Start/Stop GIF Recording",
            Action = "ToggleGifRecording",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.Number6
        },
        new HotkeyBinding
        {
            Name = "Color Picker",
            Action = "ColorPicker",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.C
        },
        new HotkeyBinding
        {
            Name = "Screen Color Picker",
            Action = "ScreenColorPicker",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.X
        },
        new HotkeyBinding
        {
            Name = "Ruler",
            Action = "Ruler",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.R
        },
        new HotkeyBinding
        {
            Name = "Auto Capture",
            Action = "AutoCapture",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.Number7
        },
        new HotkeyBinding
        {
            Name = "Scrolling Capture",
            Action = "ScrollingCapture",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.Number8
        },
        new HotkeyBinding
        {
            Name = "OCR",
            Action = "OCR",
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
            Key = VirtualKey.O
        }
    };
}
