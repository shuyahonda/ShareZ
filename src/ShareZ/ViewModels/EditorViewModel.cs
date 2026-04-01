using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using ShareZ.Models;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.UI;

namespace ShareZ.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _imagePath;

    [ObservableProperty]
    private CanvasBitmap? _sourceImage;

    [ObservableProperty]
    private ShapeType _selectedTool = ShapeType.Rectangle;

    [ObservableProperty]
    private Color _strokeColor = Color.FromArgb(255, 255, 0, 0);

    [ObservableProperty]
    private Color _fillColor = Color.FromArgb(0, 0, 0, 0);

    [ObservableProperty]
    private float _strokeWidth = 2f;

    [ObservableProperty]
    private float _fontSize = 16f;

    [ObservableProperty]
    private string _textInput = string.Empty;

    [ObservableProperty]
    private float _blurAmount = 10f;

    [ObservableProperty]
    private int _pixelSize = 10;

    [ObservableProperty]
    private bool _isFilled;

    [ObservableProperty]
    private int _stepCounter = 1;

    public ObservableCollection<AnnotationShape> Shapes { get; } = new();
    private readonly Stack<AnnotationShape> _undoStack = new();

    [RelayCommand]
    private void Undo()
    {
        if (Shapes.Count > 0)
        {
            var shape = Shapes[^1];
            _undoStack.Push(shape);
            Shapes.RemoveAt(Shapes.Count - 1);
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_undoStack.Count > 0)
        {
            var shape = _undoStack.Pop();
            Shapes.Add(shape);
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        Shapes.Clear();
        _undoStack.Clear();
        StepCounter = 1;
    }

    public AnnotationShape CreateShape(Vector2 startPoint)
    {
        var shape = new AnnotationShape
        {
            Type = SelectedTool,
            StartPoint = startPoint,
            EndPoint = startPoint,
            StrokeColor = StrokeColor,
            FillColor = FillColor,
            StrokeWidth = StrokeWidth,
            FontSize = FontSize,
            Text = TextInput,
            IsFilled = IsFilled,
            BlurAmount = BlurAmount,
            PixelSize = PixelSize
        };

        if (SelectedTool == ShapeType.Step)
        {
            shape.StepNumber = StepCounter++;
        }

        return shape;
    }

    public void AddShape(AnnotationShape shape)
    {
        Shapes.Add(shape);
        _undoStack.Clear();
    }

    [RelayCommand]
    private async Task SaveImage()
    {
        if (ImagePath == null) return;

        var savePath = ImagePath;
        // Could show save dialog here
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveImageAs()
    {
        // Show save file picker
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
        picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });
        picker.FileTypeChoices.Add("JPEG Image", new List<string> { ".jpg", ".jpeg" });
        picker.FileTypeChoices.Add("BMP Image", new List<string> { ".bmp" });
        picker.SuggestedFileName = Path.GetFileNameWithoutExtension(ImagePath ?? "screenshot");

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            ImagePath = file.Path;
        }
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (ImagePath != null)
        {
            App.ClipboardService.CopyImage(ImagePath);
        }
    }

    [RelayCommand]
    private async Task Upload()
    {
        if (ImagePath != null)
        {
            var result = await App.UploadService.UploadAsync(ImagePath);
            if (result?.IsSuccess == true && !string.IsNullOrEmpty(result.Url))
            {
                App.ClipboardService.CopyUrl(result.Url);
            }
        }
    }
}
