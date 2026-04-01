using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ShareZ.Services;

public class ClipboardService
{
    public void CopyImage(string filePath)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(
            StorageFile.GetFileFromPathAsync(filePath).AsTask().Result));
        Clipboard.SetContent(dataPackage);
        Clipboard.Flush();
    }

    public void CopyText(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
        Clipboard.Flush();
    }

    public void CopyFilePath(string filePath)
    {
        CopyText(filePath);
    }

    public void CopyUrl(string url)
    {
        CopyText(url);
    }

    public async Task<string?> GetTextAsync()
    {
        var content = Clipboard.GetContent();
        if (content.Contains(StandardDataFormats.Text))
        {
            return await content.GetTextAsync();
        }
        return null;
    }

    public async Task<SoftwareBitmap?> GetImageAsync()
    {
        var content = Clipboard.GetContent();
        if (content.Contains(StandardDataFormats.Bitmap))
        {
            var reference = await content.GetBitmapAsync();
            using var stream = await reference.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            return await decoder.GetSoftwareBitmapAsync();
        }
        return null;
    }
}
