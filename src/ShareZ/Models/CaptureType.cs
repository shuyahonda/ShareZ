namespace ShareZ.Models;

public enum CaptureType
{
    Fullscreen,
    Window,
    Region,
    ScrollingCapture,
    AutoCapture,
    LastRegion,
    Monitor
}

public enum AfterCaptureTask
{
    None = 0,
    CopyImageToClipboard = 1,
    SaveToFile = 2,
    OpenInEditor = 4,
    Upload = 8,
    PrintImage = 16,
    CopyFilePath = 32,
    CopyUrl = 64,
    ShowNotification = 128
}

public enum ImageFormat
{
    Png,
    Jpg,
    Bmp,
    Gif,
    Tiff,
    WebP
}

public enum RecordingFormat
{
    Mp4,
    Gif,
    Webm
}
