using ShareZ.Models;

namespace ShareZ.Services.Capture;

public class AutoCaptureService
{
    private System.Threading.Timer? _timer;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public event EventHandler<string>? CaptureCompleted;
    public event EventHandler<int>? CaptureCountChanged;

    private int _captureCount;

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _captureCount = 0;

        _timer = new System.Threading.Timer(
            async _ => await CaptureTimerCallback(),
            null,
            0,
            App.Settings.AutoCaptureInterval);
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
    }

    private async Task CaptureTimerCallback()
    {
        if (!_isRunning) return;

        try
        {
            string? filePath = App.Settings.AutoCaptureType switch
            {
                CaptureType.Fullscreen => await App.CaptureService.CaptureFullscreenAsync(),
                CaptureType.Window => await App.CaptureService.CaptureWindowAsync(),
                _ => await App.CaptureService.CaptureFullscreenAsync()
            };

            if (filePath != null)
            {
                _captureCount++;
                CaptureCompleted?.Invoke(this, filePath);
                CaptureCountChanged?.Invoke(this, _captureCount);
            }
        }
        catch
        {
            // Continue capturing on error
        }
    }

    public void SetInterval(int intervalMs)
    {
        _timer?.Change(0, intervalMs);
    }
}
