using ComicReaderApp;
using Microsoft.Maui.Controls;

namespace ComicReaderApp.Tests.Shared;

/// <summary>In-memory <see cref="IComicSource"/> with a fixed page count.</summary>
public sealed class FakeComicSource : IComicSource
{
    public int PageCount { get; }
    public List<int> RequestedPages { get; } = new();

    public FakeComicSource(int pageCount) => PageCount = pageCount;

    public Task<ImageSource> GetPageAsync(int pageIndex)
    {
        RequestedPages.Add(pageIndex);
        // Return null rather than a real ImageSource: constructing any Microsoft.Maui.Controls
        // BindableObject on a plain test thread throws (it needs a UI dispatcher). The view model
        // only stores/binds the value, so null is fine for testing paging logic.
        return Task.FromResult<ImageSource>(null!);
    }
}

/// <summary>
/// <see cref="IComicSourceFactory"/> for tests: hands back a preset <see cref="FakeComicSource"/>,
/// or throws <see cref="Exception"/> set via <see cref="CreateException"/> to model an unreadable /
/// unsupported file.
/// </summary>
public sealed class FakeComicSourceFactory : IComicSourceFactory
{
    readonly int _pageCount;

    public Exception? CreateException { get; set; }
    public string? LastRequestedPath { get; private set; }
    public FakeComicSource? LastCreated { get; private set; }

    public FakeComicSourceFactory(int pageCount = 3) => _pageCount = pageCount;

    public Task<IComicSource> CreateAsync(string filePath)
    {
        LastRequestedPath = filePath;
        if (CreateException is not null) throw CreateException;

        LastCreated = new FakeComicSource(_pageCount);
        return Task.FromResult<IComicSource>(LastCreated);
    }
}
