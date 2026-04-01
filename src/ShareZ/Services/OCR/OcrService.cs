using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace ShareZ.Services.OCR;

public class OcrService
{
    public async Task<string> RecognizeTextAsync(string imagePath)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(imagePath);
            using var stream = await file.OpenReadAsync();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            return await RecognizeFromBitmapAsync(bitmap);
        }
        catch (Exception ex)
        {
            return $"OCR Error: {ex.Message}";
        }
    }

    public async Task<string> RecognizeFromBitmapAsync(SoftwareBitmap bitmap)
    {
        var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (ocrEngine == null)
        {
            // Try English as fallback
            var language = new Windows.Globalization.Language("en-US");
            ocrEngine = OcrEngine.TryCreateFromLanguage(language);
        }

        if (ocrEngine == null)
        {
            return "OCR engine not available. Please install language packs.";
        }

        var result = await ocrEngine.RecognizeAsync(bitmap);
        return result.Text;
    }

    public static IReadOnlyList<Windows.Globalization.Language> GetAvailableLanguages()
    {
        return OcrEngine.AvailableRecognizerLanguages;
    }

    public async Task<List<OcrTextBlock>> RecognizeWithPositionsAsync(string imagePath)
    {
        var blocks = new List<OcrTextBlock>();

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(imagePath);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages()
                ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));

            if (ocrEngine == null) return blocks;

            var result = await ocrEngine.RecognizeAsync(bitmap);

            foreach (var line in result.Lines)
            {
                foreach (var word in line.Words)
                {
                    blocks.Add(new OcrTextBlock
                    {
                        Text = word.Text,
                        BoundingRect = word.BoundingRect
                    });
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return blocks;
    }
}

public class OcrTextBlock
{
    public string Text { get; set; } = string.Empty;
    public Windows.Foundation.Rect BoundingRect { get; set; }
}
