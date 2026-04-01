using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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

    public Rect? SelectedRegion { get; private set; }

    public RegionCaptureWindow()
    {
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

        // Maximize
        var displayArea = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Primary);
        this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            displayArea.WorkArea.X, displayArea.WorkArea.Y,
            displayArea.WorkArea.Width, displayArea.WorkArea.Height));

        // Handle ESC key
        this.Content.KeyDown += (s, e) =>
        {
            if (e.Key == VirtualKey.Escape)
            {
                this.Close();
            }
        };

        // Ensure focus for keyboard input
        this.Content.Focus(FocusState.Programmatic);
    }

    private void OverlayCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var width = (float)sender.ActualWidth;
        var height = (float)sender.ActualHeight;

        // Semi-transparent overlay
        ds.FillRectangle(0, 0, width, height, Color.FromArgb(100, 0, 0, 0));

        if (_isSelecting || _isCompleted)
        {
            var left = Math.Min(_startPoint.X, _currentPoint.X);
            var top = Math.Min(_startPoint.Y, _currentPoint.Y);
            var selWidth = Math.Abs(_currentPoint.X - _startPoint.X);
            var selHeight = Math.Abs(_currentPoint.Y - _startPoint.Y);

            // Clear the selection area (make it transparent)
            ds.FillRectangle(left, top, selWidth, selHeight, Color.FromArgb(0, 0, 0, 0));

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

            // Close overlay and perform capture
            this.Close();

            var path = await App.CaptureService.CaptureRegionAsync(SelectedRegion.Value);
            if (path != null)
            {
                await App.TaskService.ExecuteAfterCaptureAsync(path, CaptureType.Region);
            }
        }
        else
        {
            _isCompleted = false;
            InfoPanel.Visibility = Visibility.Visible;
            SizeInfo.Visibility = Visibility.Collapsed;
        }
    }
}
