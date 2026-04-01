using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShareZ.ViewModels;
using ShareZ.Views.Pages;

namespace ShareZ;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        this.InitializeComponent();

        // Set custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

        // Navigate to capture page by default
        ContentFrame.Navigate(typeof(CapturePage));

        // Register hotkey handler
        if (App.HotkeyService != null)
        {
            App.HotkeyService.HotkeyPressed += OnHotkeyPressed;
        }

        // Update recording status
        if (App.RecordingService != null)
        {
            App.RecordingService.RecordingStarted += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    RecordingStatus.Visibility = Visibility.Visible;
                    RecordingStatus.Text = "REC 00:00";
                    RecordIcon.Glyph = "\uE71A"; // Stop icon
                });
            };

            App.RecordingService.RecordingProgress += (s, duration) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    RecordingStatus.Text = $"REC {duration:mm\\:ss}";
                });
            };

            App.RecordingService.RecordingStopped += (s, path) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    RecordingStatus.Visibility = Visibility.Collapsed;
                    RecordIcon.Glyph = "\uE7C8"; // Record icon
                    StatusText.Text = $"Recording saved: {Path.GetFileName(path)}";
                });
            };
        }
    }

    private async void OnHotkeyPressed(object? sender, string action)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            switch (action)
            {
                case "CaptureFullscreen":
                    await _viewModel.CaptureFullscreenCommand.ExecuteAsync(null);
                    StatusText.Text = _viewModel.StatusText;
                    break;
                case "CaptureWindow":
                    await _viewModel.CaptureWindowCommand.ExecuteAsync(null);
                    StatusText.Text = _viewModel.StatusText;
                    break;
                case "CaptureRegion":
                    await _viewModel.CaptureRegionCommand.ExecuteAsync(null);
                    break;
                case "ToggleRecording":
                    await _viewModel.ToggleRecordingCommand.ExecuteAsync(null);
                    break;
                case "ToggleGifRecording":
                    await _viewModel.ToggleGifRecordingCommand.ExecuteAsync(null);
                    break;
                case "ColorPicker":
                case "ScreenColorPicker":
                    ContentFrame.Navigate(typeof(ToolsPage));
                    break;
                case "OCR":
                    // Capture region then OCR
                    break;
            }
        });
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var item = args.SelectedItem as NavigationViewItem;
        if (item == null) return;

        var tag = item.Tag?.ToString();
        ContentFrame.Navigate(tag switch
        {
            "capture" => typeof(CapturePage),
            "recording" => typeof(RecordingPage),
            "editor" => typeof(EditorPage),
            "aftercapture" => typeof(AfterCapturePage),
            "upload" => typeof(UploadPage),
            "tools" => typeof(ToolsPage),
            "history" => typeof(HistoryPage),
            _ => typeof(CapturePage)
        });
    }

    private async void CaptureRegion_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CaptureRegionCommand.ExecuteAsync(null);
    }

    private async void CaptureFullscreen_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CaptureFullscreenCommand.ExecuteAsync(null);
        StatusText.Text = _viewModel.StatusText;
    }

    private async void CaptureWindow_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CaptureWindowCommand.ExecuteAsync(null);
        StatusText.Text = _viewModel.StatusText;
    }

    private async void ToggleRecording_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.ToggleRecordingCommand.ExecuteAsync(null);
    }
}
