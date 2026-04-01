using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShareZ.Models;
using ShareZ.Services;

namespace ShareZ.Views.Pages;

public sealed partial class AfterCapturePage : Page
{
    public AfterCapturePage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var s = App.Settings;
        CopyToClipboardToggle.IsOn = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.CopyImageToClipboard);
        SaveToFileToggle.IsOn = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.SaveToFile);
        OpenInEditorToggle.IsOn = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.OpenInEditor);
        UploadToggle.IsOn = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.Upload);
        CopyFilePathToggle.IsOn = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.CopyFilePath);
        ShowNotificationToggle.IsOn = s.AfterCaptureTasks.HasFlag(AfterCaptureTask.ShowNotification);
        PlaySoundToggle.IsOn = s.PlaySoundAfterCapture;
        FolderPath.Text = s.ScreenshotFolder;
        FilePattern.Text = s.ScreenshotFilePattern;
        ImageFormatCombo.SelectedIndex = (int)s.DefaultImageFormat;
        JpegQualitySlider.Value = s.JpegQuality;
        JpegQualityText.Text = $"{s.JpegQuality}%";
    }

    private async void Task_Toggled(object sender, RoutedEventArgs e)
    {
        var tasks = AfterCaptureTask.None;
        if (CopyToClipboardToggle.IsOn) tasks |= AfterCaptureTask.CopyImageToClipboard;
        if (SaveToFileToggle.IsOn) tasks |= AfterCaptureTask.SaveToFile;
        if (OpenInEditorToggle.IsOn) tasks |= AfterCaptureTask.OpenInEditor;
        if (UploadToggle.IsOn) tasks |= AfterCaptureTask.Upload;
        if (CopyFilePathToggle.IsOn) tasks |= AfterCaptureTask.CopyFilePath;
        if (ShowNotificationToggle.IsOn) tasks |= AfterCaptureTask.ShowNotification;

        App.Settings.AfterCaptureTasks = tasks;
        App.Settings.PlaySoundAfterCapture = PlaySoundToggle.IsOn;
        await SettingsService.SaveAsync(App.Settings);
    }

    private async void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            FolderPath.Text = folder.Path;
            App.Settings.ScreenshotFolder = folder.Path;
            await SettingsService.SaveAsync(App.Settings);
        }
    }
}
