using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShareZ.Models;
using ShareZ.Services;

namespace ShareZ.Views.Pages;

public sealed partial class UploadPage : Page
{
    public UploadPage()
    {
        this.InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        DestinationsList.ItemsSource = App.Settings.UploadDestinations;

        var imgur = App.Settings.UploadDestinations.FirstOrDefault(d => d.Type == "Imgur");
        if (imgur != null)
        {
            ImgurClientId.Text = imgur.ApiKey;
        }
    }

    private void AddDestination_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CustomName.Text) || string.IsNullOrWhiteSpace(CustomUrl.Text)) return;

        var dest = new UploadDestination
        {
            Name = CustomName.Text,
            Type = "Custom",
            ApiUrl = CustomUrl.Text,
            ResponseUrlPattern = CustomResponsePattern.Text
        };

        if (!string.IsNullOrWhiteSpace(CustomHeaders.Text))
        {
            try
            {
                var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(CustomHeaders.Text);
                if (headers != null) dest.Headers = headers;
            }
            catch { /* ignore parse errors */ }
        }

        App.Settings.UploadDestinations.Add(dest);
        DestinationsList.ItemsSource = null;
        DestinationsList.ItemsSource = App.Settings.UploadDestinations;
    }

    private void RemoveDestination_Click(object sender, RoutedEventArgs e)
    {
        if (DestinationsList.SelectedItem is UploadDestination dest)
        {
            App.Settings.UploadDestinations.Remove(dest);
            DestinationsList.ItemsSource = null;
            DestinationsList.ItemsSource = App.Settings.UploadDestinations;
        }
    }

    private void SetDefault_Click(object sender, RoutedEventArgs e)
    {
        if (DestinationsList.SelectedItem is UploadDestination dest)
        {
            foreach (var d in App.Settings.UploadDestinations) d.IsDefault = false;
            dest.IsDefault = true;
            DestinationsList.ItemsSource = null;
            DestinationsList.ItemsSource = App.Settings.UploadDestinations;
        }
    }

    private async void SaveUploadSettings_Click(object sender, RoutedEventArgs e)
    {
        var imgur = App.Settings.UploadDestinations.FirstOrDefault(d => d.Type == "Imgur");
        if (imgur != null)
        {
            imgur.ApiKey = ImgurClientId.Text;
        }

        await SettingsService.SaveAsync(App.Settings);
        UploadStatus.Text = "Settings saved!";
    }

    private async void UploadFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".bmp");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            UploadStatus.Text = "Uploading...";
            var result = await App.UploadService.UploadAsync(file.Path);
            if (result?.IsSuccess == true)
            {
                App.ClipboardService.CopyUrl(result.Url);
                UploadStatus.Text = $"Uploaded! URL copied: {result.Url}";
            }
            else
            {
                UploadStatus.Text = $"Upload failed: {result?.ErrorMessage}";
            }
        }
    }

    private async void UploadClipboard_Click(object sender, RoutedEventArgs e)
    {
        UploadStatus.Text = "Getting clipboard image...";
        // Would need to save clipboard image to temp file first, then upload
        UploadStatus.Text = "Clipboard upload not yet implemented";
    }
}
