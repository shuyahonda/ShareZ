using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareZ.Interop;
using System.Security.Cryptography;
using System.Text;
using Windows.UI;

namespace ShareZ.ViewModels;

public partial class ToolsViewModel : ObservableObject
{
    // Color Picker
    [ObservableProperty] private Color _selectedColor = Colors.Red;
    [ObservableProperty] private string _hexColor = "#FF0000";
    [ObservableProperty] private string _rgbColor = "255, 0, 0";
    [ObservableProperty] private string _hslColor = "0, 100%, 50%";

    // Hash Check
    [ObservableProperty] private string _hashFilePath = string.Empty;
    [ObservableProperty] private string _md5Hash = string.Empty;
    [ObservableProperty] private string _sha1Hash = string.Empty;
    [ObservableProperty] private string _sha256Hash = string.Empty;
    [ObservableProperty] private string _sha512Hash = string.Empty;
    [ObservableProperty] private string _compareHash = string.Empty;
    [ObservableProperty] private string _hashMatchResult = string.Empty;

    // QR Code
    [ObservableProperty] private string _qrText = string.Empty;
    [ObservableProperty] private string _qrDecodeResult = string.Empty;

    // Ruler
    [ObservableProperty] private int _rulerStartX;
    [ObservableProperty] private int _rulerStartY;
    [ObservableProperty] private int _rulerEndX;
    [ObservableProperty] private int _rulerEndY;
    [ObservableProperty] private double _rulerDistance;

    // DNS Lookup
    [ObservableProperty] private string _dnsHostname = string.Empty;
    [ObservableProperty] private string _dnsResult = string.Empty;

    partial void OnSelectedColorChanged(Color value)
    {
        HexColor = $"#{value.R:X2}{value.G:X2}{value.B:X2}";
        RgbColor = $"{value.R}, {value.G}, {value.B}";

        // Convert to HSL
        double r = value.R / 255.0, g = value.G / 255.0, b = value.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double h = 0, s, l = (max + min) / 2;

        if (max == min) { h = s = 0; }
        else
        {
            double d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h /= 6;
        }

        HslColor = $"{(int)(h * 360)}, {(int)(s * 100)}%, {(int)(l * 100)}%";
    }

    [RelayCommand]
    private void CopyHexColor() => App.ClipboardService.CopyText(HexColor);

    [RelayCommand]
    private void CopyRgbColor() => App.ClipboardService.CopyText(RgbColor);

    [RelayCommand]
    private void CopyHslColor() => App.ClipboardService.CopyText(HslColor);

    [RelayCommand]
    private void PickScreenColor()
    {
        NativeMethods.GetCursorPos(out var point);
        IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
        uint pixel = NativeMethods.GetPixel(hdc, point.X, point.Y);
        NativeMethods.ReleaseDC(IntPtr.Zero, hdc);

        byte r = (byte)(pixel & 0xFF);
        byte g = (byte)((pixel >> 8) & 0xFF);
        byte b = (byte)((pixel >> 16) & 0xFF);

        SelectedColor = Color.FromArgb(255, r, g, b);
    }

    // Hash Check
    [RelayCommand]
    private async Task BrowseHashFile()
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
        picker.FileTypeFilter.Add("*");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            HashFilePath = file.Path;
            await ComputeHashesAsync(file.Path);
        }
    }

    [RelayCommand]
    private async Task ComputeHashes()
    {
        if (!string.IsNullOrEmpty(HashFilePath) && File.Exists(HashFilePath))
        {
            await ComputeHashesAsync(HashFilePath);
        }
    }

    private async Task ComputeHashesAsync(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);

        Md5Hash = Convert.ToHexString(MD5.HashData(bytes));
        Sha1Hash = Convert.ToHexString(SHA1.HashData(bytes));
        Sha256Hash = Convert.ToHexString(SHA256.HashData(bytes));
        Sha512Hash = Convert.ToHexString(SHA512.HashData(bytes));

        CheckHashMatch();
    }

    partial void OnCompareHashChanged(string value)
    {
        CheckHashMatch();
    }

    private void CheckHashMatch()
    {
        if (string.IsNullOrEmpty(CompareHash))
        {
            HashMatchResult = string.Empty;
            return;
        }

        var compare = CompareHash.Replace(" ", "").ToUpperInvariant();
        if (compare == Md5Hash || compare == Sha1Hash || compare == Sha256Hash || compare == Sha512Hash)
        {
            HashMatchResult = "Match found!";
        }
        else
        {
            HashMatchResult = "No match";
        }
    }

    // DNS Lookup
    [RelayCommand]
    private async Task DnsLookup()
    {
        if (string.IsNullOrWhiteSpace(DnsHostname)) return;

        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(DnsHostname);
            var sb = new StringBuilder();
            sb.AppendLine($"DNS Lookup for: {DnsHostname}");
            foreach (var addr in addresses)
            {
                sb.AppendLine($"  {addr}");
            }
            DnsResult = sb.ToString();
        }
        catch (Exception ex)
        {
            DnsResult = $"Error: {ex.Message}";
        }
    }

    // Ruler
    [RelayCommand]
    private void CalculateDistance()
    {
        double dx = RulerEndX - RulerStartX;
        double dy = RulerEndY - RulerStartY;
        RulerDistance = Math.Sqrt(dx * dx + dy * dy);
    }
}
