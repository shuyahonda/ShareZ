using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using ShareZ.Models;
using ShareZ.ViewModels;
using System.Numerics;
using Windows.UI;

namespace ShareZ.Views.Pages;

public sealed partial class EditorPage : Page
{
    private readonly EditorViewModel _viewModel = new();
    private CanvasBitmap? _sourceImage;
    private AnnotationShape? _currentShape;
    private bool _isDrawing;

    public EditorPage()
    {
        this.InitializeComponent();
        DataContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string filePath && !string.IsNullOrEmpty(filePath))
        {
            await LoadImageAsync(filePath);
        }
    }

    public async Task LoadImageAsync(string filePath)
    {
        _viewModel.ImagePath = filePath;
        var device = CanvasDevice.GetSharedDevice();
        _sourceImage = await CanvasBitmap.LoadAsync(device, filePath);
        _viewModel.SourceImage = _sourceImage;

        EditorCanvas.Width = _sourceImage.SizeInPixels.Width;
        EditorCanvas.Height = _sourceImage.SizeInPixels.Height;
        ImageSize.Text = $"{_sourceImage.SizeInPixels.Width} x {_sourceImage.SizeInPixels.Height}";
        EditorStatus.Text = Path.GetFileName(filePath);

        EditorCanvas.Invalidate();
    }

    private void EditorCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Resources created on demand
    }

    private void EditorCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;

        // Draw checkered background
        DrawCheckerboard(ds, (float)sender.ActualWidth, (float)sender.ActualHeight);

        // Draw source image
        if (_sourceImage != null)
        {
            ds.DrawImage(_sourceImage);
        }

        // Draw all completed annotations
        foreach (var shape in _viewModel.Shapes)
        {
            DrawShape(ds, shape);
        }

        // Draw current shape being drawn
        if (_currentShape != null && _isDrawing)
        {
            DrawShape(ds, _currentShape);
        }
    }

    private void DrawCheckerboard(CanvasDrawingSession ds, float width, float height)
    {
        int size = 16;
        var color1 = Color.FromArgb(255, 204, 204, 204);
        var color2 = Color.FromArgb(255, 255, 255, 255);

        for (int y = 0; y < height; y += size)
        {
            for (int x = 0; x < width; x += size)
            {
                var color = ((x / size + y / size) % 2 == 0) ? color1 : color2;
                ds.FillRectangle(x, y, size, size, color);
            }
        }
    }

    private void DrawShape(CanvasDrawingSession ds, AnnotationShape shape)
    {
        var topLeft = shape.TopLeft;

        switch (shape.Type)
        {
            case ShapeType.Rectangle:
                if (shape.IsFilled)
                    ds.FillRectangle(topLeft.X, topLeft.Y, shape.Width, shape.Height, shape.FillColor);
                ds.DrawRectangle(topLeft.X, topLeft.Y, shape.Width, shape.Height,
                    shape.StrokeColor, shape.StrokeWidth);
                break;

            case ShapeType.Ellipse:
                var center = new Vector2(topLeft.X + shape.Width / 2, topLeft.Y + shape.Height / 2);
                if (shape.IsFilled)
                    ds.FillEllipse(center, shape.Width / 2, shape.Height / 2, shape.FillColor);
                ds.DrawEllipse(center, shape.Width / 2, shape.Height / 2,
                    shape.StrokeColor, shape.StrokeWidth);
                break;

            case ShapeType.Line:
                ds.DrawLine(shape.StartPoint, shape.EndPoint, shape.StrokeColor, shape.StrokeWidth);
                break;

            case ShapeType.Arrow:
                DrawArrow(ds, shape);
                break;

            case ShapeType.Text:
                var format = new CanvasTextFormat
                {
                    FontSize = shape.FontSize,
                    FontFamily = "Segoe UI",
                    WordWrapping = CanvasWordWrapping.Wrap
                };
                ds.DrawText(shape.Text, topLeft.X, topLeft.Y, shape.StrokeColor, format);
                break;

            case ShapeType.Freehand:
                if (shape.FreehandPoints.Count >= 2)
                {
                    for (int i = 1; i < shape.FreehandPoints.Count; i++)
                    {
                        ds.DrawLine(shape.FreehandPoints[i - 1], shape.FreehandPoints[i],
                            shape.StrokeColor, shape.StrokeWidth);
                    }
                }
                break;

            case ShapeType.Blur:
                DrawBlurRegion(ds, shape);
                break;

            case ShapeType.Pixelate:
                DrawPixelateRegion(ds, shape);
                break;

            case ShapeType.Highlight:
                var highlightColor = Color.FromArgb(100, shape.StrokeColor.R, shape.StrokeColor.G, shape.StrokeColor.B);
                ds.FillRectangle(topLeft.X, topLeft.Y, shape.Width, shape.Height, highlightColor);
                break;

            case ShapeType.Step:
                DrawStep(ds, shape);
                break;

            case ShapeType.Crop:
                // Draw crop region indicator
                ds.DrawRectangle(topLeft.X, topLeft.Y, shape.Width, shape.Height,
                    Color.FromArgb(255, 255, 255, 255), 2, new CanvasStrokeStyle { DashStyle = CanvasDashStyle.Dash });
                break;
        }
    }

    private void DrawArrow(CanvasDrawingSession ds, AnnotationShape shape)
    {
        ds.DrawLine(shape.StartPoint, shape.EndPoint, shape.StrokeColor, shape.StrokeWidth);

        // Draw arrowhead
        var direction = Vector2.Normalize(shape.EndPoint - shape.StartPoint);
        var perpendicular = new Vector2(-direction.Y, direction.X);
        float arrowSize = shape.StrokeWidth * 4 + 8;

        var arrowPoint1 = shape.EndPoint - direction * arrowSize + perpendicular * arrowSize * 0.5f;
        var arrowPoint2 = shape.EndPoint - direction * arrowSize - perpendicular * arrowSize * 0.5f;

        using var pathBuilder = new CanvasPathBuilder(ds);
        pathBuilder.BeginFigure(shape.EndPoint);
        pathBuilder.AddLine(arrowPoint1);
        pathBuilder.AddLine(arrowPoint2);
        pathBuilder.EndFigure(CanvasFigureLoop.Closed);

        using var geometry = CanvasGeometry.CreatePath(pathBuilder);
        ds.FillGeometry(geometry, shape.StrokeColor);
    }

    private void DrawBlurRegion(CanvasDrawingSession ds, AnnotationShape shape)
    {
        if (_sourceImage == null) return;

        var topLeft = shape.TopLeft;
        try
        {
            var blur = new GaussianBlurEffect
            {
                Source = _sourceImage,
                BlurAmount = shape.BlurAmount
            };

            using var layer = ds.CreateLayer(1f,
                new Windows.Foundation.Rect(topLeft.X, topLeft.Y, shape.Width, shape.Height));
            ds.DrawImage(blur);
        }
        catch
        {
            // Fallback: draw semi-transparent overlay
            ds.FillRectangle(topLeft.X, topLeft.Y, shape.Width, shape.Height,
                Color.FromArgb(180, 128, 128, 128));
        }
    }

    private void DrawPixelateRegion(CanvasDrawingSession ds, AnnotationShape shape)
    {
        if (_sourceImage == null) return;

        var topLeft = shape.TopLeft;
        int pixelSize = shape.PixelSize;

        // Simplified pixelation: draw colored rectangles
        for (float y = topLeft.Y; y < topLeft.Y + shape.Height; y += pixelSize)
        {
            for (float x = topLeft.X; x < topLeft.X + shape.Width; x += pixelSize)
            {
                var color = Color.FromArgb(255,
                    (byte)(((int)x * 37 + (int)y * 53) % 256),
                    (byte)(((int)x * 47 + (int)y * 67) % 256),
                    (byte)(((int)x * 59 + (int)y * 71) % 256));
                ds.FillRectangle(x, y, pixelSize, pixelSize, color);
            }
        }
    }

    private void DrawStep(CanvasDrawingSession ds, AnnotationShape shape)
    {
        float radius = 16;
        var center = shape.StartPoint;

        ds.FillCircle(center, radius, shape.StrokeColor);
        ds.DrawCircle(center, radius, Color.FromArgb(255, 255, 255, 255), 2);

        var format = new CanvasTextFormat
        {
            FontSize = 14,
            FontFamily = "Segoe UI",
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
            VerticalAlignment = CanvasVerticalAlignment.Center
        };

        ds.DrawText(shape.StepNumber.ToString(), center.X - radius, center.Y - radius,
            radius * 2, radius * 2, Color.FromArgb(255, 255, 255, 255), format);
    }

    // Pointer Events
    private void EditorCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(EditorCanvas);
        var position = new Vector2((float)point.Position.X, (float)point.Position.Y);

        _currentShape = _viewModel.CreateShape(position);
        _isDrawing = true;

        if (_currentShape.Type == ShapeType.Freehand)
        {
            _currentShape.FreehandPoints.Add(position);
        }

        EditorCanvas.CapturePointer(e.Pointer);
    }

    private void EditorCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(EditorCanvas);
        var position = new Vector2((float)point.Position.X, (float)point.Position.Y);

        CursorPosition.Text = $"{(int)position.X}, {(int)position.Y}";

        if (_isDrawing && _currentShape != null)
        {
            if (_currentShape.Type == ShapeType.Freehand)
            {
                _currentShape.FreehandPoints.Add(position);
            }
            else
            {
                _currentShape.EndPoint = position;
            }
            EditorCanvas.Invalidate();
        }
    }

    private void EditorCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDrawing && _currentShape != null)
        {
            var point = e.GetCurrentPoint(EditorCanvas);
            var position = new Vector2((float)point.Position.X, (float)point.Position.Y);
            _currentShape.EndPoint = position;

            _viewModel.AddShape(_currentShape);
            _currentShape = null;
            _isDrawing = false;
            EditorCanvas.Invalidate();
        }

        EditorCanvas.ReleasePointerCaptures();
    }

    // Toolbar Events
    private void Tool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            _viewModel.SelectedTool = Enum.Parse<ShapeType>(tag);
        }
    }

    private void StrokeColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        _viewModel.StrokeColor = args.NewColor;
    }

    private void StrokeWidth_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (StrokeWidthCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _viewModel.StrokeWidth = float.Parse(tag);
        }
    }

    private void Filled_Changed(object sender, RoutedEventArgs e)
    {
        _viewModel.IsFilled = FilledCheck.IsChecked == true;
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => _viewModel.UndoCommand.Execute(null);
    private void Redo_Click(object sender, RoutedEventArgs e) => _viewModel.RedoCommand.Execute(null);

    private void CopyClipboard_Click(object sender, RoutedEventArgs e) =>
        _viewModel.CopyToClipboardCommand.Execute(null);

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.ImagePath != null)
        {
            await SaveAnnotatedImageAsync(_viewModel.ImagePath);
            EditorStatus.Text = "Saved!";
        }
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.SaveImageAsCommand.ExecuteAsync(null);
        if (_viewModel.ImagePath != null)
        {
            await SaveAnnotatedImageAsync(_viewModel.ImagePath);
            EditorStatus.Text = $"Saved as: {Path.GetFileName(_viewModel.ImagePath)}";
        }
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.UploadCommand.ExecuteAsync(null);
    }

    private async Task SaveAnnotatedImageAsync(string filePath)
    {
        if (_sourceImage == null) return;

        var device = CanvasDevice.GetSharedDevice();
        var width = (int)_sourceImage.SizeInPixels.Width;
        var height = (int)_sourceImage.SizeInPixels.Height;

        var renderTarget = new CanvasRenderTarget(device, width, height, 96);
        using (var ds = renderTarget.CreateDrawingSession())
        {
            ds.DrawImage(_sourceImage);
            foreach (var shape in _viewModel.Shapes)
            {
                DrawShape(ds, shape);
            }
        }

        using var stream = new FileStream(filePath, FileMode.Create);
        var format = Helpers.ImageFormatHelper.GetCanvasBitmapFormat(App.Settings.DefaultImageFormat);
        await renderTarget.SaveAsync(stream.AsRandomAccessStream(), format);
    }
}
