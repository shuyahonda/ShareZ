using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;
using Windows.System;
using Windows.UI;

namespace ShareZ.Views;

public sealed partial class RulerOverlayWindow : Window
{
    private Vector2 _startPoint;
    private Vector2 _currentPoint;
    private bool _isMeasuring;

    public RulerOverlayWindow()
    {
        this.InitializeComponent();

        var presenter = this.AppWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        var displayArea = DisplayArea.GetFromWindowId(this.AppWindow.Id, DisplayAreaFallback.Primary);
        this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            displayArea.WorkArea.X, displayArea.WorkArea.Y,
            displayArea.WorkArea.Width, displayArea.WorkArea.Height));

        this.Content.KeyDown += (s, e) =>
        {
            if (e.Key == VirtualKey.Escape) this.Close();
        };

        this.Content.Focus(FocusState.Programmatic);
    }

    private void RulerCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;
        var width = (float)sender.ActualWidth;
        var height = (float)sender.ActualHeight;

        // Semi-transparent overlay
        ds.FillRectangle(0, 0, width, height, Color.FromArgb(60, 0, 0, 0));

        // Draw crosshair
        var crosshairColor = Color.FromArgb(150, 255, 255, 255);
        ds.DrawLine(0, _currentPoint.Y, width, _currentPoint.Y, crosshairColor, 1);
        ds.DrawLine(_currentPoint.X, 0, _currentPoint.X, height, crosshairColor, 1);

        if (_isMeasuring)
        {
            // Draw measurement line
            ds.DrawLine(_startPoint, _currentPoint, Color.FromArgb(255, 0, 170, 255), 2);

            // Draw endpoints
            ds.FillCircle(_startPoint, 4, Color.FromArgb(255, 255, 100, 100));
            ds.FillCircle(_currentPoint, 4, Color.FromArgb(255, 100, 255, 100));

            // Calculate distance
            var dx = _currentPoint.X - _startPoint.X;
            var dy = _currentPoint.Y - _startPoint.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            // Draw measurement text
            var midPoint = (_startPoint + _currentPoint) / 2;
            var text = $"{distance:F1} px | {Math.Abs(dx):F0} x {Math.Abs(dy):F0} | {angle:F1}°";

            ds.DrawText(text, midPoint.X, midPoint.Y - 24,
                Color.FromArgb(255, 255, 255, 255),
                new CanvasTextFormat { FontSize = 14, FontFamily = "Consolas" });

            // Draw ruler ticks along the line
            var dir = Vector2.Normalize(_currentPoint - _startPoint);
            var perp = new Vector2(-dir.Y, dir.X);
            float lineLength = (float)distance;

            for (float d = 0; d <= lineLength; d += 50)
            {
                var tickPoint = _startPoint + dir * d;
                var tickEnd = tickPoint + perp * 8;
                ds.DrawLine(tickPoint, tickEnd, Color.FromArgb(200, 255, 255, 255), 1);

                if (d > 0)
                {
                    ds.DrawText($"{d:F0}", tickPoint.X + 4, tickPoint.Y - 16,
                        Color.FromArgb(180, 255, 255, 255),
                        new CanvasTextFormat { FontSize = 9, FontFamily = "Consolas" });
                }
            }
        }

        // Cursor position
        var posText = $"({(int)_currentPoint.X}, {(int)_currentPoint.Y})";
        ds.DrawText(posText, _currentPoint.X + 16, _currentPoint.Y + 16,
            Color.FromArgb(200, 255, 255, 255),
            new CanvasTextFormat { FontSize = 11, FontFamily = "Consolas" });
    }

    private void RulerCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(RulerCanvas);
        _startPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
        _currentPoint = _startPoint;
        _isMeasuring = true;
        RulerCanvas.CapturePointer(e.Pointer);
    }

    private void RulerCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(RulerCanvas);
        _currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
        RulerCanvas.Invalidate();

        if (_isMeasuring)
        {
            var dx = _currentPoint.X - _startPoint.X;
            var dy = _currentPoint.Y - _startPoint.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            MeasureInfo.Visibility = Visibility.Visible;
            MeasureText.Text = $"Distance: {distance:F1} px  |  {Math.Abs(dx):F0} x {Math.Abs(dy):F0}";
        }
    }

    private void RulerCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isMeasuring = false;
        RulerCanvas.ReleasePointerCaptures();
        RulerCanvas.Invalidate();
    }
}
