namespace ComicReaderApp;

/// <summary>Production <see cref="IComicSourceFactory"/>: dispatches on the file extension.</summary>
public class ComicSourceFactory : IComicSourceFactory
{
    public async Task<IComicSource> CreateAsync(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".pdf" => await PdfComicSource.LoadAsync(filePath),
            _ => throw new NotSupportedException($"Unsupported format: {filePath}")
        };
}
