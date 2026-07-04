namespace ComicReaderApp;

public interface IComicSource
{
    int PageCount { get; }
    Task<ImageSource> GetPageAsync(int pageIndex);
}