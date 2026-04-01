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
                await CaptureUsingGdiAsync(filePath);
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
                await CaptureUsingGdiAsync(fullScreenPath);
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

    private async Task CaptureUsingGdiAsync(string filePath)
    {
        // Fallback GDI-based screen capture
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

        var canvasDevice = CanvasDevice.GetSharedDevice();
        var renderTarget = new CanvasRenderTarget(canvasDevice, width, height, 96);

        using (var ds = renderTarget.CreateDrawingSession())
        {
            ds.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        }

        var format = ImageFormatHelper.GetCanvasBitmapFormat(App.Settings.DefaultImageFormat);
        using var stream = new FileStream(filePath, FileMode.Create);
        await renderTarget.SaveAsync(stream.AsRandomAccessStream(), format);
    }

    private async Task CaptureWindowUsingGdiAsync(IntPtr hwnd, string filePath)
    {
        NativeMethods.GetWindowRect(hwnd, out var rect);
        int width = rect.Width;
        int height = rect.Height;

        var canvasDevice = CanvasDevice.GetSharedDevice();
        var renderTarget = new CanvasRenderTarget(canvasDevice, width, height, 96);

        using (var ds = renderTarget.CreateDrawingSession())
        {
            ds.Clear(Windows.UI.Color.FromArgb(255, 240, 240, 240));
        }

        var format = ImageFormatHelper.GetCanvasBitmapFormat(App.Settings.DefaultImageFormat);
        using var stream = new FileStream(filePath, FileMode.Create);
        await renderTarget.SaveAsync(stream.AsRandomAccessStream(), format);
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
