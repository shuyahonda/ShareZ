using Microsoft.Graphics.Canvas;
using ShareZ.Helpers;
using ShareZ.Interop;
using ShareZ.Models;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ShareZ.Services.Capture;

public class ScreenCaptureService
{
    public event EventHandler<string>? CaptureCompleted;
    public event EventHandler<string>? CaptureError;

    /// <summary>
    /// Pre-captures the full screen using GDI BitBlt and saves to a temp PNG file.
    /// Returns the temp file path for use as overlay background.
    /// </summary>
    public async Task<string?> CaptureScreenToTempFileAsync()
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"sharez_precap_{Guid.NewGuid():N}.png");
            await CaptureScreenUsingGdiAsync(tempPath);
            return File.Exists(tempPath) ? tempPath : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Saves a cropped region from a pre-captured image file.
    /// </summary>
    public async Task<string?> SaveCroppedRegionAsync(string sourceImagePath, Windows.Foundation.Rect region)
    {
        try
        {
            var filePath = FileNameHelper.GenerateFilePath(
                App.Settings.ScreenshotFolder,
                App.Settings.ScreenshotFilePattern,
                App.Settings.DefaultImageFormat);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            await CropImageAsync(sourceImagePath, filePath, region);

            CaptureCompleted?.Invoke(this, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(this, ex.Message);
            return null;
        }
    }

    public async Task<string?> CaptureFullscreenAsync()
    {
        try
        {
            var filePath = FileNameHelper.GenerateFilePath(
                App.Settings.ScreenshotFolder,
                App.Settings.ScreenshotFilePattern,
                App.Settings.DefaultImageFormat);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            // Use Windows.Graphics.Capture API to capture all monitors
            var item = CaptureHelper.CreateItemForMonitor(
                CaptureHelper.GetPrimaryMonitorHandle());

            if (item == null)
            {
                // Fallback: use GDI-based capture
                await CaptureScreenUsingGdiAsync(filePath);
            }
            else
            {
                await CaptureItemAsync(item, filePath);
            }

            CaptureCompleted?.Invoke(this, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(this, ex.Message);
            return null;
        }
    }

    public async Task<string?> CaptureWindowAsync(IntPtr hwnd = default)
    {
        try
        {
            if (hwnd == IntPtr.Zero)
            {
                hwnd = NativeMethods.GetForegroundWindow();
            }

            var filePath = FileNameHelper.GenerateFilePath(
                App.Settings.ScreenshotFolder,
                App.Settings.ScreenshotFilePattern,
                App.Settings.DefaultImageFormat);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                await CaptureItemAsync(item, filePath);
            }
            else
            {
                await CaptureWindowUsingGdiAsync(hwnd, filePath);
            }

            CaptureCompleted?.Invoke(this, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(this, ex.Message);
            return null;
        }
    }

    public async Task<string?> CaptureRegionAsync(Windows.Foundation.Rect region)
    {
        try
        {
            var filePath = FileNameHelper.GenerateFilePath(
                App.Settings.ScreenshotFolder,
                App.Settings.ScreenshotFilePattern,
                App.Settings.DefaultImageFormat);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            // Capture full screen first, then crop
            var fullScreenPath = filePath + ".tmp.png";
            var monitorHandle = CaptureHelper.GetPrimaryMonitorHandle();
            var item = CaptureHelper.CreateItemForMonitor(monitorHandle);

            if (item != null)
            {
                await CaptureItemAsync(item, fullScreenPath);
            }
            else
            {
                await CaptureScreenUsingGdiAsync(fullScreenPath);
            }

            // Crop to region
            await CropImageAsync(fullScreenPath, filePath, region);

            // Clean up temp file
            if (File.Exists(fullScreenPath))
                File.Delete(fullScreenPath);

            CaptureCompleted?.Invoke(this, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(this, ex.Message);
            return null;
        }
    }

    public async Task<string?> CaptureMonitorAsync(IntPtr monitorHandle)
    {
        try
        {
            var filePath = FileNameHelper.GenerateFilePath(
                App.Settings.ScreenshotFolder,
                App.Settings.ScreenshotFilePattern,
                App.Settings.DefaultImageFormat);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var item = CaptureHelper.CreateItemForMonitor(monitorHandle);
            if (item != null)
            {
                await CaptureItemAsync(item, filePath);
            }

            CaptureCompleted?.Invoke(this, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            CaptureError?.Invoke(this, ex.Message);
            return null;
        }
    }

    private async Task CaptureItemAsync(GraphicsCaptureItem item, string filePath)
    {
        var canvasDevice = new CanvasDevice();

        using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            canvasDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            item.Size);

        using var session = framePool.CreateCaptureSession(item);

        var tcs = new TaskCompletionSource<CanvasBitmap>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        framePool.FrameArrived += (s, a) =>
        {
            using var frame = s.TryGetNextFrame();
            if (frame != null)
            {
                var bitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, frame.Surface);
                tcs.TrySetResult(bitmap);
            }
        };

        session.IsCursorCaptureEnabled = App.Settings.CaptureCursor;
        session.StartCapture();

        try
        {
            var bitmap = await tcs.Task;
            session.Dispose();

            var format = ImageFormatHelper.GetCanvasBitmapFormat(App.Settings.DefaultImageFormat);
            using var stream = new FileStream(filePath, FileMode.Create);
            await bitmap.SaveAsync(stream.AsRandomAccessStream(), format,
                App.Settings.DefaultImageFormat == ImageFormat.Jpg
                    ? App.Settings.JpegQuality / 100f
                    : 1f);
        }
        catch (OperationCanceledException)
        {
            session.Dispose();
            throw new Exception("Capture timed out");
        }
    }

    /// <summary>
    /// Captures the full virtual screen using GDI BitBlt (reliable fallback).
    /// </summary>
    private async Task CaptureScreenUsingGdiAsync(string filePath)
    {
        int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
        int x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        int y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);

        if (width <= 0 || height <= 0)
        {
            width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            x = 0;
            y = 0;
        }

        // Capture using GDI BitBlt
        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        var memDc = NativeMethods.CreateCompatibleDC(screenDc);
        var hBitmap = NativeMethods.CreateCompatibleBitmap(screenDc, width, height);
        var oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

        NativeMethods.BitBlt(memDc, 0, 0, width, height, screenDc, x, y, NativeMethods.SRCCOPY);

        NativeMethods.SelectObject(memDc, oldBitmap);

        // Extract pixel data from HBITMAP
        var bmi = new NativeMethods.BITMAPINFO
        {
            bmiHeader = new NativeMethods.BITMAPINFOHEADER
            {
                biSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // Negative for top-down DIB
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0 // BI_RGB
            }
        };

        var pixelData = new byte[width * height * 4];
        NativeMethods.GetDIBits(memDc, hBitmap, 0, (uint)height, pixelData, ref bmi, NativeMethods.DIB_RGB_COLORS);

        // Clean up GDI resources
        NativeMethods.DeleteObject(hBitmap);
        NativeMethods.DeleteDC(memDc);
        NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);

        // Convert BGRA pixel data to PNG using BitmapEncoder
        using var outputStream = new FileStream(filePath, FileMode.Create);
        var encoder = await BitmapEncoder.CreateAsync(
            BitmapEncoder.PngEncoderId,
            outputStream.AsRandomAccessStream());

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)width, (uint)height,
            96, 96,
            pixelData);

        await encoder.FlushAsync();
    }

    /// <summary>
    /// Captures a specific window using GDI BitBlt.
    /// </summary>
    private async Task CaptureWindowUsingGdiAsync(IntPtr hwnd, string filePath)
    {
        NativeMethods.GetWindowRect(hwnd, out var rect);
        int width = rect.Width;
        int height = rect.Height;

        if (width <= 0 || height <= 0) return;

        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        var memDc = NativeMethods.CreateCompatibleDC(screenDc);
        var hBitmap = NativeMethods.CreateCompatibleBitmap(screenDc, width, height);
        var oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

        NativeMethods.BitBlt(memDc, 0, 0, width, height, screenDc, rect.Left, rect.Top, NativeMethods.SRCCOPY);

        NativeMethods.SelectObject(memDc, oldBitmap);

        var bmi = new NativeMethods.BITMAPINFO
        {
            bmiHeader = new NativeMethods.BITMAPINFOHEADER
            {
                biSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0
            }
        };

        var pixelData = new byte[width * height * 4];
        NativeMethods.GetDIBits(memDc, hBitmap, 0, (uint)height, pixelData, ref bmi, NativeMethods.DIB_RGB_COLORS);

        NativeMethods.DeleteObject(hBitmap);
        NativeMethods.DeleteDC(memDc);
        NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);

        using var outputStream = new FileStream(filePath, FileMode.Create);
        var encoder = await BitmapEncoder.CreateAsync(
            BitmapEncoder.PngEncoderId,
            outputStream.AsRandomAccessStream());

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)width, (uint)height,
            96, 96,
            pixelData);

        await encoder.FlushAsync();
    }

    private async Task CropImageAsync(string sourcePath, string destPath, Windows.Foundation.Rect region)
    {
        var file = await StorageFile.GetFileFromPathAsync(sourcePath);
        using var inputStream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(inputStream);

        var transform = new BitmapTransform
        {
            Bounds = new BitmapBounds
            {
                X = (uint)Math.Max(0, region.X),
                Y = (uint)Math.Max(0, region.Y),
                Width = (uint)Math.Min(region.Width, decoder.PixelWidth - region.X),
                Height = (uint)Math.Min(region.Height, decoder.PixelHeight - region.Y)
            }
        };

        var pixels = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

        using var outputStream = new FileStream(destPath, FileMode.Create);
        var encoder = await BitmapEncoder.CreateAsync(
            ImageFormatHelper.GetBitmapEncoderId(App.Settings.DefaultImageFormat),
            outputStream.AsRandomAccessStream());

        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform.Bounds.Width,
            transform.Bounds.Height,
            96, 96,
            pixels.DetachPixelData());

        await encoder.FlushAsync();
    }
}
