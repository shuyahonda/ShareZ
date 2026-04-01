using Microsoft.Graphics.Canvas;
using ShareZ.Helpers;
using ShareZ.Models;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;

namespace ShareZ.Services.Recording;

public class ScreenRecordingService : IDisposable
{
    private GraphicsCaptureItem? _captureItem;
    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession? _session;
    private CanvasDevice? _canvasDevice;
    private bool _isRecording;
    private DateTime _startTime;
    private readonly List<RecordedFrame> _frames = new();
    private RecordingFormat _currentFormat;
    private CancellationTokenSource? _recordingCts;

    public bool IsRecording => _isRecording;
    public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _startTime : TimeSpan.Zero;

    public event EventHandler? RecordingStarted;
    public event EventHandler<string>? RecordingStopped;
    public event EventHandler<TimeSpan>? RecordingProgress;
    public event EventHandler<string>? RecordingError;

    public async Task StartRecordingAsync(RecordingFormat format = RecordingFormat.Mp4)
    {
        if (_isRecording) return;

        try
        {
            _currentFormat = format;
            _canvasDevice = new CanvasDevice();
            _frames.Clear();

            // Get capture item for primary monitor
            var monitorHandle = CaptureHelper.GetPrimaryMonitorHandle();
            _captureItem = CaptureHelper.CreateItemForMonitor(monitorHandle);

            if (_captureItem == null)
            {
                RecordingError?.Invoke(this, "Failed to create capture item");
                return;
            }

            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                _canvasDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                _captureItem.Size);

            _framePool.FrameArrived += OnFrameArrived;

            _session = _framePool.CreateCaptureSession(_captureItem);
            _session.IsCursorCaptureEnabled = App.Settings.CaptureCursor;

            _isRecording = true;
            _startTime = DateTime.Now;
            _recordingCts = new CancellationTokenSource();

            _session.StartCapture();

            // Start progress timer
            _ = UpdateProgressAsync(_recordingCts.Token);

            RecordingStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _isRecording = false;
            RecordingError?.Invoke(this, ex.Message);
        }
    }

    public async Task<string?> StopRecordingAsync()
    {
        if (!_isRecording) return null;

        _isRecording = false;
        _recordingCts?.Cancel();

        _session?.Dispose();
        _framePool?.Dispose();
        _session = null;
        _framePool = null;

        try
        {
            var filePath = FileNameHelper.GenerateRecordingFilePath(
                App.Settings.RecordingFolder, _currentFormat);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            if (_currentFormat == RecordingFormat.Gif)
            {
                await SaveAsGifAsync(filePath);
            }
            else
            {
                await SaveAsVideoAsync(filePath);
            }

            _frames.Clear();
            RecordingStopped?.Invoke(this, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            RecordingError?.Invoke(this, ex.Message);
            return null;
        }
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        if (!_isRecording) return;

        using var frame = sender.TryGetNextFrame();
        if (frame == null) return;

        try
        {
            var bitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice!, frame.Surface);
            var timestamp = DateTime.Now - _startTime;

            lock (_frames)
            {
                _frames.Add(new RecordedFrame
                {
                    Bitmap = bitmap,
                    Timestamp = timestamp,
                    Size = frame.ContentSize
                });
            }
        }
        catch
        {
            // Skip frame on error
        }
    }

    private async Task UpdateProgressAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRecording)
        {
            RecordingProgress?.Invoke(this, RecordingDuration);
            try { await Task.Delay(100, ct); } catch { break; }
        }
    }

    private async Task SaveAsGifAsync(string filePath)
    {
        // Save frames as an animated GIF using Win2D
        if (_frames.Count == 0) return;

        var device = CanvasDevice.GetSharedDevice();
        int frameDelay = 1000 / App.Settings.GifFps;

        // Sample frames at the desired FPS
        var sampledFrames = SampleFrames(_frames, App.Settings.GifFps);

        if (sampledFrames.Count == 0) return;

        // Create the GIF file with individual frame PNGs combined
        // Using a basic approach: save as individual frames and combine
        using var stream = new FileStream(filePath, FileMode.Create);
        var randomAccessStream = stream.AsRandomAccessStream();

        var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
            Windows.Graphics.Imaging.BitmapEncoder.GifEncoderId, randomAccessStream);

        foreach (var frame in sampledFrames)
        {
            var pixels = frame.Bitmap.GetPixelBytes();
            encoder.SetPixelData(
                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
                frame.Bitmap.SizeInPixels.Width,
                frame.Bitmap.SizeInPixels.Height,
                96, 96, pixels);

            if (frame != sampledFrames.Last())
            {
                await encoder.GoToNextFrameAsync();
            }
        }

        await encoder.FlushAsync();
    }

    private async Task SaveAsVideoAsync(string filePath)
    {
        // For MP4, save frames as individual images and use a simple container
        // Full MediaFoundation encoding would be needed for production use
        // For now, save as a series of frames or a large animated image
        if (_frames.Count == 0) return;

        var sampledFrames = SampleFrames(_frames, App.Settings.VideoFps);

        // Save as individual PNGs in a temp directory for now
        // A full implementation would use MediaFoundation for MP4 encoding
        var tempDir = Path.Combine(Path.GetTempPath(), "ShareZ_Recording_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        for (int i = 0; i < sampledFrames.Count; i++)
        {
            var framePath = Path.Combine(tempDir, $"frame_{i:D6}.png");
            using var frameStream = new FileStream(framePath, FileMode.Create);
            await sampledFrames[i].Bitmap.SaveAsync(
                frameStream.AsRandomAccessStream(),
                CanvasBitmapFileFormat.Png);
        }

        // Move the frame directory to the output path for now
        // In production, would encode to MP4 using MediaFoundation
        var infoFile = filePath + ".info.txt";
        await File.WriteAllTextAsync(infoFile,
            $"Frames: {sampledFrames.Count}\nFPS: {App.Settings.VideoFps}\nFrames directory: {tempDir}");

        // Save first frame as a preview
        if (sampledFrames.Count > 0)
        {
            using var previewStream = new FileStream(filePath, FileMode.Create);
            await sampledFrames[0].Bitmap.SaveAsync(
                previewStream.AsRandomAccessStream(),
                CanvasBitmapFileFormat.Png);
        }
    }

    private List<RecordedFrame> SampleFrames(List<RecordedFrame> frames, int targetFps)
    {
        if (frames.Count == 0) return new();

        var sampled = new List<RecordedFrame>();
        var interval = TimeSpan.FromMilliseconds(1000.0 / targetFps);
        var nextTime = TimeSpan.Zero;

        foreach (var frame in frames.OrderBy(f => f.Timestamp))
        {
            if (frame.Timestamp >= nextTime)
            {
                sampled.Add(frame);
                nextTime += interval;
            }
        }

        return sampled;
    }

    public void Dispose()
    {
        _isRecording = false;
        _recordingCts?.Cancel();
        _session?.Dispose();
        _framePool?.Dispose();
        _canvasDevice?.Dispose();

        foreach (var frame in _frames)
        {
            frame.Bitmap.Dispose();
        }
        _frames.Clear();
    }

    private class RecordedFrame
    {
        public CanvasBitmap Bitmap { get; set; } = null!;
        public TimeSpan Timestamp { get; set; }
        public SizeInt32 Size { get; set; }
    }
}
