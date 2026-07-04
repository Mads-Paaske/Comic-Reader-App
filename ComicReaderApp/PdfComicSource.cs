using SkiaSharp;
using Conversion = PDFtoImage.Conversion;

namespace ComicReaderApp;

public class PdfComicSource : IComicSource
{
    readonly List<SKBitmap> _pages = new();

    public int PageCount => _pages.Count;

    public static async Task<PdfComicSource> LoadAsync(string filePath)
    {
        var source = new PdfComicSource();
        await using var stream = File.OpenRead(filePath);

        // Materializes every page as an SKBitmap up front.
        await foreach (var bitmap in Conversion.ToImagesAsync(stream))
        {
            source._pages.Add(bitmap);
        }

        return source;
    }

    public Task<ImageSource> GetPageAsync(int pageIndex)
    {
        var bitmap = _pages[pageIndex];
        var ms = new MemoryStream();
        bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
        ms.Position = 0;
        return Task.FromResult<ImageSource>(ImageSource.FromStream(() => ms));
    }
}