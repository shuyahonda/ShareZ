using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShareZ.Models;
using ShareZ.Services;
using ShareZ.ViewModels;

namespace ShareZ.Views.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel = new();

    public SettingsPage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        _viewModel.LoadSettings();

        StartWithWindowsToggle.IsOn = _viewModel.StartWithWindows;
        MinimizeToTrayToggle.IsOn = _viewModel.MinimizeToTray;
        ShowTrayIconToggle.IsOn = _viewModel.ShowTrayIcon;

        ScreenshotFolderBox.Text = _viewModel.ScreenshotFolder;
        FilePatternBox.Text = _viewModel.ScreenshotFilePattern;
        ImageFormatBox.SelectedIndex = _viewModel.SelectedImageFormatIndex;
        JpegQualityBox.Value = _viewModel.JpegQuality;
        JpegQualityLabel.Text = $"{_viewModel.JpegQuality}%";
        CaptureCursorToggle.IsOn = _viewModel.CaptureCursor;

        RecordingFolderBox.Text = _viewModel.RecordingFolder;
        VideoFpsBox.Value = _viewModel.VideoFps;
        GifFpsBox.Value = _viewModel.GifFps;
        RecordAudioToggle.IsOn = _viewModel.RecordAudio;

        HotkeyList.ItemsSource = _viewModel.Hotkeys;

        AutoCaptureIntervalBox.Value = App.Settings.AutoCaptureInterval;
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.StartWithWindows = StartWithWindowsToggle.IsOn;
        _viewModel.MinimizeToTray = MinimizeToTrayToggle.IsOn;
        _viewModel.ShowTrayIcon = ShowTrayIconToggle.IsOn;

        _viewModel.ScreenshotFolder = ScreenshotFolderBox.Text;
        _viewModel.ScreenshotFilePattern = FilePatternBox.Text;
        _viewModel.SelectedImageFormatIndex = ImageFormatBox.SelectedIndex;
        _viewModel.JpegQuality = (int)JpegQualityBox.Value;
        _viewModel.CaptureCursor = CaptureCursorToggle.IsOn;

        _viewModel.RecordingFolder = RecordingFolderBox.Text;
        _viewModel.VideoFps = (int)VideoFpsBox.Value;
        _viewModel.GifFps = (int)GifFpsBox.Value;
        _viewModel.RecordAudio = RecordAudioToggle.IsOn;

        App.Settings.AutoCaptureInterval = (int)AutoCaptureIntervalBox.Value;

        await _viewModel.SaveSettingsCommand.ExecuteAsync(null);

        // Re-register hotkeys
        App.HotkeyService?.UnregisterAll();
        App.HotkeyService?.RegisterDefaultHotkeys(App.Settings);
    }

    private async void BrowseScreenshotFolder_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BrowseScreenshotFolderCommand.ExecuteAsync(null);
        ScreenshotFolderBox.Text = _viewModel.ScreenshotFolder;
    }

    private async void BrowseRecordingFolder_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BrowseRecordingFolderCommand.ExecuteAsync(null);
        RecordingFolderBox.Text = _viewModel.RecordingFolder;
    }
}
