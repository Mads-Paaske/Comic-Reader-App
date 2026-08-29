using ComicReaderApp;

namespace ComicReaderApp.Tests.Shared;

/// <summary>
/// In-memory <see cref="IBookRepository"/> for tests. Mimics sqlite-net semantics:
/// AddBookAsync assigns an auto-incrementing Id, Update/Delete match on Id and
/// return the number of affected rows (0 when the book is not present).
/// Set the Throw* flags to exercise failure paths.
/// </summary>
public sealed class FakeBookRepository : IBookRepository
{
    readonly List<Book> _books = new();
    int _nextId = 1;

    public bool ThrowOnGetAll { get; set; }
    public bool ThrowOnAdd { get; set; }
    public bool ThrowOnUpdate { get; set; }
    public bool ThrowOnDelete { get; set; }

    public int AddCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }

    public IReadOnlyList<Book> Snapshot => _books;

    public FakeBookRepository(params Book[] seed)
    {
        foreach (var book in seed)
        {
            if (book.Id == 0) book.Id = _nextId++;
            else _nextId = Math.Max(_nextId, book.Id + 1);
            _books.Add(book);
        }
    }

    public Task<List<Book>> GetAllBooksAsync()
    {
        if (ThrowOnGetAll)
            throw new InvalidOperationException("Simulated database read failure.");

        // Return copies so callers can't mutate our store by reference.
        return Task.FromResult(_books.Select(Clone).ToList());
    }

    public Task<int> AddBookAsync(Book book)
    {
        AddCallCount++;
        if (ThrowOnAdd)
            throw new InvalidOperationException("Simulated insert failure.");

        book.Id = _nextId++;
        _books.Add(Clone(book));
        return Task.FromResult(1);
    }

    public Task<int> UpdateBookAsync(Book book)
    {
        UpdateCallCount++;
        if (ThrowOnUpdate)
            throw new InvalidOperationException("Simulated update failure.");

        var existing = _books.FirstOrDefault(b => b.Id == book.Id);
        if (existing is null)
            return Task.FromResult(0);

        _books.Remove(existing);
        _books.Add(Clone(book));
        return Task.FromResult(1);
    }

    public Task<int> DeleteBookAsync(Book book)
    {
        DeleteCallCount++;
        if (ThrowOnDelete)
            throw new InvalidOperationException("Simulated delete failure.");

        var removed = _books.RemoveAll(b => b.Id == book.Id);
        return Task.FromResult(removed);
    }

    static Book Clone(Book b) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,
        Year = b.Year,
        Publisher = b.Publisher,
        Isbn = b.Isbn,
        FilePath = b.FilePath,
        CoverImagePath = b.CoverImagePath,
    };
}
