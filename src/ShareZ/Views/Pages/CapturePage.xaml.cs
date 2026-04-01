using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShareZ.Models;
using ShareZ.Services.Capture;
using ShareZ.ViewModels;

namespace ShareZ.Views.Pages;

public sealed partial class CapturePage : Page
{
    private readonly MainViewModel _viewModel = new();
    private readonly AutoCaptureService _autoCaptureService = new();

    public CapturePage()
    {
        this.InitializeComponent();
        LoadRecentCaptures();
    }

    private async void LoadRecentCaptures()
    {
        await App.HistoryService.LoadAsync();
        var recent = App.HistoryService.Items.Take(10).ToList();
        RecentCapturesList.ItemsSource = recent;
    }

    private async void CaptureFullscreen_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CaptureFullscreenCommand.ExecuteAsync(null);
    }

    private async void CaptureWindow_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CaptureWindowCommand.ExecuteAsync(null);
    }

    private async void CaptureRegion_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CaptureRegionCommand.ExecuteAsync(null);
    }

    private async void CaptureLastRegion_Click(object sender, RoutedEventArgs e)
    {
        // Capture with last used region
        await _viewModel.CaptureFullscreenCommand.ExecuteAsync(null);
    }

    private async void ScrollingCapture_Click(object sender, RoutedEventArgs e)
    {
        var scrollService = new ScrollingCaptureService();
        var hwnd = Interop.NativeMethods.GetForegroundWindow();
        var path = await scrollService.CaptureAsync(hwnd);
        if (path != null)
        {
            await App.TaskService.ExecuteAfterCaptureAsync(path, CaptureType.ScrollingCapture);
        }
    }

    private void AutoCapture_Click(object sender, RoutedEventArgs e)
    {
        if (_autoCaptureService.IsRunning)
        {
            _autoCaptureService.Stop();
        }
        else
        {
            _autoCaptureService.Start();
        }
    }

    private void RecentCapturesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentCapturesList.SelectedItem is HistoryItem item && File.Exists(item.FilePath))
        {
            Frame.Navigate(typeof(EditorPage), item.FilePath);
        }
    }
}
