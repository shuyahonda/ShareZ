using Microsoft.UI.Xaml;
using ShareZ.Helpers;
using ShareZ.Services;
using ShareZ.Services.Capture;
using ShareZ.Services.Recording;
using ShareZ.Services.Upload;
using ShareZ.Services.OCR;

namespace ShareZ;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public static AppSettings Settings { get; private set; } = new();
    public static HotkeyService HotkeyService { get; private set; } = null!;
    public static ScreenCaptureService CaptureService { get; private set; } = null!;
    public static ScreenRecordingService RecordingService { get; private set; } = null!;
    public static UploadService UploadService { get; private set; } = null!;
    public static OcrService OcrService { get; private set; } = null!;
    public static ClipboardService ClipboardService { get; private set; } = null!;
    public static TaskService TaskService { get; private set; } = null!;
    public static HistoryService HistoryService { get; private set; } = null!;
    public static TrayIconService TrayIconService { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load settings
        Settings = await SettingsService.LoadAsync();

        // Initialize services
        CaptureService = new ScreenCaptureService();
        RecordingService = new ScreenRecordingService();
        UploadService = new UploadService(Settings);
        OcrService = new OcrService();
        ClipboardService = new ClipboardService();
        TaskService = new TaskService(Settings);
        HistoryService = new HistoryService();

        // Create main window
        MainWindow = new MainWindow();
        MainWindow.Activate();

        // Initialize hotkeys after window is ready
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
        HotkeyService = new HotkeyService(hwnd);
        HotkeyService.RegisterDefaultHotkeys(Settings);

        // Initialize tray icon
        TrayIconService = new TrayIconService(hwnd);
        TrayIconService.Initialize();
    }
}
