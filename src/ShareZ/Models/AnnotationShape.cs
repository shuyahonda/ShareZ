using System.Numerics;
using Windows.UI;

namespace ShareZ.Models;

public enum ShapeType
{
    Rectangle,
    Ellipse,
    Line,
    Arrow,
    Text,
    Freehand,
    Blur,
    Pixelate,
    Highlight,
    Step,
    Crop
}

public class AnnotationShape
{
    public ShapeType Type { get; set; }
    public Vector2 StartPoint { get; set; }
    public Vector2 EndPoint { get; set; }
    public Color StrokeColor { get; set; } = Color.FromArgb(255, 255, 0, 0);
    public Color FillColor { get; set; } = Colors.Transparent;
    public float StrokeWidth { get; set; } = 2f;
    public string Text { get; set; } = string.Empty;
    public float FontSize { get; set; } = 16f;
    public bool IsFilled { get; set; }
    public int StepNumber { get; set; }
    public float BlurAmount { get; set; } = 10f;
    public int PixelSize { get; set; } = 10;
    public List<Vector2> FreehandPoints { get; set; } = new();

    public float Width => Math.Abs(EndPoint.X - StartPoint.X);
    public float Height => Math.Abs(EndPoint.Y - StartPoint.Y);
    public Vector2 TopLeft => new(Math.Min(StartPoint.X, EndPoint.X), Math.Min(StartPoint.Y, EndPoint.Y));
}
