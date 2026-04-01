using Microsoft.Graphics.Canvas;
using ShareZ.Helpers;
using ShareZ.Interop;
using ShareZ.Models;
using System.Runtime.InteropServices;

namespace ShareZ.Services.Capture;

public class ScrollingCaptureService
{
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_VSCROLL = 0x0115;
    private const int SB_LINEDOWN = 1;

    [DllImport("user32.dll")]
    private static extern bool SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public event EventHandler<string>? CaptureCompleted;
    public event EventHandler<int>? ScrollProgress;

    private bool _isCancelled;

    public void Cancel() => _isCancelled = true;

    public async Task<string?> CaptureAsync(IntPtr windowHandle)
    {
        _isCancelled = false;
        var capturedFrames = new List<string>();

        try
        {
            var settings = App.Settings;
            var filePath = FileNameHelper.GenerateFilePath(
                settings.ScreenshotFolder,
                settings.ScreenshotFilePattern,
                settings.DefaultImageFormat);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            // Capture frames while scrolling
            for (int i = 0; i < settings.MaxScrollCount && !_isCancelled; i++)
            {
                var framePath = $"{filePath}.frame{i}.png";
                var captureService = App.CaptureService;
                await captureService.CaptureWindowAsync(windowHandle);

                // Scroll down
                SendMessage(windowHandle, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
                await Task.Delay(settings.ScrollDelay);

                ScrollProgress?.Invoke(this, i + 1);
                capturedFrames.Add(framePath);
            }

            // Stitch images together
            if (capturedFrames.Count > 0)
            {
                await StitchImagesAsync(capturedFrames, filePath);

                // Clean up frame files
                foreach (var frame in capturedFrames)
                {
                    if (File.Exists(frame)) File.Delete(frame);
                }

                CaptureCompleted?.Invoke(this, filePath);
                return filePath;
            }

            return null;
        }
        catch (Exception)
        {
            // Clean up any captured frames
            foreach (var frame in capturedFrames)
            {
                if (File.Exists(frame)) File.Delete(frame);
            }
            throw;
        }
    }

    private async Task StitchImagesAsync(List<string> framePaths, string outputPath)
    {
        var device = CanvasDevice.GetSharedDevice();
        var bitmaps = new List<CanvasBitmap>();

        foreach (var path in framePaths)
        {
            if (File.Exists(path))
            {
                var bitmap = await CanvasBitmap.LoadAsync(device, path);
                bitmaps.Add(bitmap);
            }
        }

        if (bitmaps.Count == 0) return;

        int totalWidth = (int)bitmaps.Max(b => b.SizeInPixels.Width);
        int totalHeight = (int)bitmaps.Sum(b => b.SizeInPixels.Height);

        var renderTarget = new CanvasRenderTarget(device, totalWidth, totalHeight, 96);
        using (var ds = renderTarget.CreateDrawingSession())
        {
            ds.Clear(Windows.UI.Color.FromArgb(255, 255, 255, 255));
            float yOffset = 0;
            foreach (var bitmap in bitmaps)
            {
                ds.DrawImage(bitmap, 0, yOffset);
                yOffset += bitmap.SizeInPixels.Height;
            }
        }

        var format = ImageFormatHelper.GetCanvasBitmapFormat(App.Settings.DefaultImageFormat);
        using var stream = new FileStream(outputPath, FileMode.Create);
        await renderTarget.SaveAsync(stream.AsRandomAccessStream(), format);

        foreach (var bitmap in bitmaps)
        {
            bitmap.Dispose();
        }
    }
}
