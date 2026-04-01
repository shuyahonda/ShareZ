using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ShareZ.ViewModels;
using Windows.UI;

namespace ShareZ.Views.Pages;

public sealed partial class ToolsPage : Page
{
    private readonly ToolsViewModel _viewModel = new();

    public ToolsPage()
    {
        this.InitializeComponent();
    }

    // Color Picker
    private void ColorPicker_Changed(ColorPicker sender, ColorChangedEventArgs args)
    {
        _viewModel.SelectedColor = args.NewColor;
        HexText.Text = _viewModel.HexColor;
        RgbText.Text = _viewModel.RgbColor;
        HslText.Text = _viewModel.HslColor;
        ColorPreview.Background = new SolidColorBrush(args.NewColor);
    }

    private void PickScreenColor_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.PickScreenColorCommand.Execute(null);
        MainColorPicker.Color = _viewModel.SelectedColor;
    }

    private void CopyHex_Click(object sender, RoutedEventArgs e) => _viewModel.CopyHexColorCommand.Execute(null);
    private void CopyRgb_Click(object sender, RoutedEventArgs e) => _viewModel.CopyRgbColorCommand.Execute(null);
    private void CopyHsl_Click(object sender, RoutedEventArgs e) => _viewModel.CopyHslColorCommand.Execute(null);

    // OCR
    private async void CaptureOcr_Click(object sender, RoutedEventArgs e)
    {
        OcrResultText.Text = "Capturing and recognizing...";
        var path = await App.CaptureService.CaptureFullscreenAsync();
        if (path != null)
        {
            var text = await App.OcrService.RecognizeTextAsync(path);
            OcrResultText.Text = text;
        }
        else
        {
            OcrResultText.Text = "Capture failed";
        }
    }

    private async void OcrFromFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".bmp");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            OcrResultText.Text = "Recognizing...";
            var text = await App.OcrService.RecognizeTextAsync(file.Path);
            OcrResultText.Text = text;
        }
    }

    private async void OcrFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        var bitmap = await App.ClipboardService.GetImageAsync();
        if (bitmap != null)
        {
            OcrResultText.Text = "Recognizing...";
            var text = await App.OcrService.RecognizeFromBitmapAsync(bitmap);
            OcrResultText.Text = text;
        }
        else
        {
            OcrResultText.Text = "No image in clipboard";
        }
    }

    private void CopyOcrText_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(OcrResultText.Text))
        {
            App.ClipboardService.CopyText(OcrResultText.Text);
        }
    }

    // Hash Check
    private async void HashBrowse_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.BrowseHashFileCommand.ExecuteAsync(null);
        HashFileName.Text = Path.GetFileName(_viewModel.HashFilePath);
        Md5Text.Text = _viewModel.Md5Hash;
        Sha1Text.Text = _viewModel.Sha1Hash;
        Sha256Text.Text = _viewModel.Sha256Hash;
        Sha512Text.Text = _viewModel.Sha512Hash;
    }

    // QR Code
    private void GenerateQr_Click(object sender, RoutedEventArgs e)
    {
        // QR generation would need ZXing.Net or similar library
        QrDecodeResult.Text = "QR code generation requires ZXing.Net library";
    }

    private void DecodeQr_Click(object sender, RoutedEventArgs e)
    {
        QrDecodeResult.Text = "QR decode from screen capture";
    }

    // Ruler
    private void OpenRuler_Click(object sender, RoutedEventArgs e)
    {
        var rulerWindow = new Views.RulerOverlayWindow();
        rulerWindow.Activate();
    }

    // DNS Lookup
    private async void DnsLookup_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DnsHostname = DnsHostInput.Text;
        await _viewModel.DnsLookupCommand.ExecuteAsync(null);
        DnsResultText.Text = _viewModel.DnsResult;
    }
}
