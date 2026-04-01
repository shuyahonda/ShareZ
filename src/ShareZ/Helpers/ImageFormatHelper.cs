using Microsoft.Graphics.Canvas;
using ShareZ.Models;
using Windows.Graphics.Imaging;

namespace ShareZ.Helpers;

public static class ImageFormatHelper
{
    public static CanvasBitmapFileFormat GetCanvasBitmapFormat(ImageFormat format) => format switch
    {
        ImageFormat.Png => CanvasBitmapFileFormat.Png,
        ImageFormat.Jpg => CanvasBitmapFileFormat.Jpeg,
        ImageFormat.Bmp => CanvasBitmapFileFormat.Bmp,
        ImageFormat.Gif => CanvasBitmapFileFormat.Gif,
        ImageFormat.Tiff => CanvasBitmapFileFormat.Tiff,
        _ => CanvasBitmapFileFormat.Png
    };

    public static Guid GetBitmapEncoderId(ImageFormat format) => format switch
    {
        ImageFormat.Png => BitmapEncoder.PngEncoderId,
        ImageFormat.Jpg => BitmapEncoder.JpegEncoderId,
        ImageFormat.Bmp => BitmapEncoder.BmpEncoderId,
        ImageFormat.Gif => BitmapEncoder.GifEncoderId,
        ImageFormat.Tiff => BitmapEncoder.TiffEncoderId,
        _ => BitmapEncoder.PngEncoderId
    };

    public static string GetMimeType(ImageFormat format) => format switch
    {
        ImageFormat.Png => "image/png",
        ImageFormat.Jpg => "image/jpeg",
        ImageFormat.Bmp => "image/bmp",
        ImageFormat.Gif => "image/gif",
        ImageFormat.Tiff => "image/tiff",
        ImageFormat.WebP => "image/webp",
        _ => "image/png"
    };
}
