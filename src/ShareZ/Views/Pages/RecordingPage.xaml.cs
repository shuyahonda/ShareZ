using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShareZ.Models;

namespace ShareZ.Views.Pages;

public sealed partial class RecordingPage : Page
{
    public RecordingPage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        OutputFolderText.Text = App.Settings.RecordingFolder;
        FpsBox.Value = App.Settings.VideoFps;
        GifFpsBox.Value = App.Settings.GifFps;
        CaptureCursorToggle.IsOn = App.Settings.CaptureCursor;
        RecordAudioToggle.IsOn = App.Settings.RecordAudio;
    }

    private async void VideoRecord_Click(object sender, RoutedEventArgs e)
    {
        if (App.RecordingService.IsRecording)
        {
            await StopRecording();
        }
        else
        {
            await StartRecording(RecordingFormat.Mp4);
        }
    }

    private async void GifRecord_Click(object sender, RoutedEventArgs e)
    {
        if (App.RecordingService.IsRecording)
        {
            await StopRecording();
        }
        else
        {
            await StartRecording(RecordingFormat.Gif);
        }
    }

    private async Task StartRecording(RecordingFormat format)
    {
        App.Settings.VideoFps = (int)FpsBox.Value;
        App.Settings.GifFps = (int)GifFpsBox.Value;
        App.Settings.CaptureCursor = CaptureCursorToggle.IsOn;
        App.Settings.RecordAudio = RecordAudioToggle.IsOn;

        await App.RecordingService.StartRecordingAsync(format);

        RecordingTimerPanel.Visibility = Visibility.Visible;
        VideoRecordText.Text = "Stop Recording";
        GifRecordText.Text = "Stop Recording";

        App.RecordingService.RecordingProgress += (s, duration) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                RecordingTimer.Text = duration.ToString(@"hh\:mm\:ss");
            });
        };
    }

    private async Task StopRecording()
    {
        var path = await App.RecordingService.StopRecordingAsync();
        RecordingTimerPanel.Visibility = Visibility.Collapsed;
        VideoRecordText.Text = "Record Video (MP4)";
        GifRecordText.Text = "Record GIF";
        RecordingTimer.Text = "00:00:00";
    }

    private async void StopRecording_Click(object sender, RoutedEventArgs e)
    {
        await StopRecording();
    }
}
