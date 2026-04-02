using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ShareZ.Models;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace ShareZ.Views;

public sealed partial class RegionCaptureWindow : Window
{
    private Vector2 _startPoint;
    private Vector2 _currentPoint;
    private bool _isSelecting;
    private bool _isCompleted;
    private bool _isFlashing;
    private readonly string? _screenImagePath;
    private CanvasBitmap? _screenBitmap;

    public Rect? SelectedRegion { get; private set; }

    public RegionCaptureWindow(string? screenImagePath)
    {
        _screenImagePath = screenImagePath;
        this.InitializeComponent();

        // Make fullscreen and borderless
        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        // Maximize to full display area (including taskbar)
        var displayArea = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Primary);
        this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            displayArea.OuterBounds.X, displayArea.OuterBounds.Y,
            displayArea.OuterBounds.Width, displayArea.OuterBounds.Height));

        // Handle ESC key
        this.Content.KeyDown += (s, e) =>
        {
            if (e.Key == VirtualKey.Escape)
            {
                CleanupAndClose();
            }
        };

        // Ensure focus for keyboard input
        this.Content.Focus(FocusState.Programmatic);
    }

    private void OverlayCanvas_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        // Load the pre-captured screen image onto the CanvasControl's device
        if (_screenImagePath != null && File.Exists(_screenImagePath))
        {
            args.TrackAsyncAction(LoadScreenBitmapAsync(sender).AsAsyncAction());
        }
    }

    private async Task LoadScreenBitmapAsync(CanvasControl sender)
    {
        try
        {
            _screenBitmap = await CanvasBitmap.LoadAsync(sender, _screenImagePath);
            sender.Invalidate();
        }
        catch
        {
            _screenBitmap = null;
        }
    }

    private void OverlayCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var width = (float)sender.ActualWidth;
        var height = (float)sender.ActualHeight;

        // Draw the pre-captured screen image as background
        if (_screenBitmap != null)
        {
            ds.DrawImage(_screenBitmap, new Rect(0, 0, width, height),
                new Rect(0, 0, _screenBitmap.SizeInPixels.Width, _screenBitmap.SizeInPixels.Height));
        }

        // Semi-transparent overlay
        ds.FillRectangle(0, 0, width, height, Color.FromArgb(100, 0, 0, 0));

        if (_isSelecting || _isCompleted)
        {
            var left = Math.Min(_startPoint.X, _currentPoint.X);
            var top = Math.Min(_startPoint.Y, _currentPoint.Y);
            var selWidth = Math.Abs(_currentPoint.X - _startPoint.X);
            var selHeight = Math.Abs(_currentPoint.Y - _startPoint.Y);

            if (_screenBitmap != null)
            {
                // Draw the clear (unshaded) region from the pre-captured image
                var scaleX = (float)_screenBitmap.SizeInPixels.Width / width;
                var scaleY = (float)_screenBitmap.SizeInPixels.Height / height;
                ds.DrawImage(_screenBitmap,
                    new Rect(left, top, selWidth, selHeight),
                    new Rect(left * scaleX, top * scaleY, selWidth * scaleX, selHeight * scaleY));
            }
            else
            {
                // Fallback: clear selection area
                ds.FillRectangle(left, top, selWidth, selHeight, Color.FromArgb(0, 0, 0, 0));
            }

            if (_isFlashing)
            {
                // Flash: white overlay over selection
                ds.FillRectangle(left, top, selWidth, selHeight, Color.FromArgb(200, 255, 255, 255));
            }
            else
            {
                // Draw selection border
                ds.DrawRectangle(left, top, selWidth, selHeight,
                    Color.FromArgb(255, 0, 170, 255), 2);

                // Draw crosshair lines
                var crosshairColor = Color.FromArgb(100, 255, 0, 0);
                ds.DrawLine(0, _currentPoint.Y, width, _currentPoint.Y, crosshairColor, 1);
                ds.DrawLine(_currentPoint.X, 0, _currentPoint.X, height, crosshairColor, 1);

                // Draw dimension text near selection
                var dimText = $"{(int)selWidth} x {(int)selHeight}";
                ds.DrawText(dimText, left + 4, top - 20,
                    Color.FromArgb(255, 255, 255, 255),
                    new Microsoft.Graphics.Canvas.Text.CanvasTextFormat
                    {
                        FontSize = 12,
                        FontFamily = "Consolas"
                    });
            }
        }
        else
        {
            // Draw crosshair at cursor when not selecting
            var crosshairColor = Color.FromArgb(150, 255, 255, 255);
            ds.DrawLine(0, _currentPoint.Y, width, _currentPoint.Y, crosshairColor, 1);
            ds.DrawLine(_currentPoint.X, 0, _currentPoint.X, height, crosshairColor, 1);
        }
    }

    private void OverlayCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(OverlayCanvas);
        _startPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
        _currentPoint = _startPoint;
        _isSelecting = true;
        InfoPanel.Visibility = Visibility.Collapsed;
        SizeInfo.Visibility = Visibility.Visible;
        OverlayCanvas.CapturePointer(e.Pointer);
    }

    private void OverlayCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(OverlayCanvas);
        _currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);

        if (_isSelecting)
        {
            var w = Math.Abs(_currentPoint.X - _startPoint.X);
            var h = Math.Abs(_currentPoint.Y - _startPoint.Y);
            SizeText.Text = $"{(int)w} x {(int)h}";

            Canvas.SetLeft(SizeInfo, Math.Min(_startPoint.X, _currentPoint.X));
            Canvas.SetTop(SizeInfo, Math.Max(_startPoint.Y, _currentPoint.Y) + 4);
        }

        OverlayCanvas.Invalidate();
    }

    private async void OverlayCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSelecting) return;

        _isSelecting = false;
        _isCompleted = true;
        OverlayCanvas.ReleasePointerCaptures();

        var left = Math.Min(_startPoint.X, _currentPoint.X);
        var top = Math.Min(_startPoint.Y, _currentPoint.Y);
        var width = Math.Abs(_currentPoint.X - _startPoint.X);
        var height = Math.Abs(_currentPoint.Y - _startPoint.Y);

        if (width > 5 && height > 5)
        {
            SelectedRegion = new Rect(left, top, width, height);

            // Calculate pixel coordinates before closing
            Rect? pixelRegion = null;
            if (_screenBitmap != null)
            {
                var displayArea = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Primary);
                var displayWidth = (float)displayArea.OuterBounds.Width;
                var displayHeight = (float)displayArea.OuterBounds.Height;
                var scaleX = (float)_screenBitmap.SizeInPixels.Width / displayWidth;
                var scaleY = (float)_screenBitmap.SizeInPixels.Height / displayHeight;

                pixelRegion = new Rect(
                    left * scaleX,
                    top * scaleY,
                    width * scaleX,
                    height * scaleY);
            }

            // Flash the selected region before closing
            _isFlashing = true;
            OverlayCanvas.Invalidate();
            await Task.Delay(150);

            // Close overlay
            this.Close();

            if (_screenImagePath != null && pixelRegion != null)
            {
                // Crop from the pre-captured image (no re-capture needed)
                var path = await App.CaptureService.SaveCroppedRegionAsync(_screenImagePath, pixelRegion.Value);

                // Clean up temp file
                try { File.Delete(_screenImagePath); } catch { }

                if (path != null)
                {
                    await App.TaskService.ExecuteAfterCaptureAsync(path, CaptureType.Region);
                }
            }
            else
            {
                // Fallback: capture live
                var path = await App.CaptureService.CaptureRegionAsync(SelectedRegion.Value);
                if (path != null)
                {
                    await App.TaskService.ExecuteAfterCaptureAsync(path, CaptureType.Region);
                }
            }
        }
        else
        {
            _isCompleted = false;
            InfoPanel.Visibility = Visibility.Visible;
            SizeInfo.Visibility = Visibility.Collapsed;
        }
    }

    private void CleanupAndClose()
    {
        // Clean up temp file on cancel
        if (_screenImagePath != null)
        {
            try { File.Delete(_screenImagePath); } catch { }
        }
        this.Close();
    }
}
