using ComicReaderApp;
using Xunit;

namespace ComicReaderApp.Tests.Backend;

/// <summary>
/// Tests for <see cref="BookDatabase"/> against a real SQLite file (one throwaway DB per test,
/// via the test-only <c>BookDatabase(string dbPath)</c> constructor). Covers the CRUD round-trip,
/// affected-row counts for hits and misses, and cross-connection persistence.
/// </summary>
public class BookDatabaseTests : IDisposable
{
    readonly List<string> _paths = new();

    string NewPath()
    {
        string path = Path.Combine(Path.GetTempPath(), $"crtest_{Guid.NewGuid():N}.db3");
        _paths.Add(path);
        return path;
    }

    BookDatabase NewDatabase() => new(NewPath());

    static Book MakeBook(string title = "Bone") => new()
    {
        Title = title,
        Author = "Jeff Smith",
        Year = "1991",
        Publisher = "Cartoon Books",
        Isbn = "9780888craft",
        FilePath = $@"C:\comics\{title}.pdf",
        CoverImagePath = $@"C:\covers\{title}.png",
    };

    public void Dispose()
    {
        foreach (var path in _paths)
        {
            try { File.Delete(path); }
            catch (IOException) { /* connection still pooled open; it's a temp file */ }
        }
    }

    // ---------------------------------------------------------------------
    //  Reads on an empty database
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAllBooksAsync_OnNewDatabase_ReturnsEmpty()
    {
        var db = NewDatabase();

        var books = await db.GetAllBooksAsync();

        Assert.Empty(books);
    }

    // ---------------------------------------------------------------------
    //  Insert
    // ---------------------------------------------------------------------

    [Fact]
    public async Task AddBookAsync_ReturnsOne_AndAssignsAutoIncrementId()
    {
        var db = NewDatabase();
        var book = MakeBook();

        int rows = await db.AddBookAsync(book);

        Assert.Equal(1, rows);
        Assert.True(book.Id > 0);
    }

    [Fact]
    public async Task AddBookAsync_ThenGetAll_RoundTripsEveryField()
    {
        var db = NewDatabase();
        var book = MakeBook("Maus");

        await db.AddBookAsync(book);
        var loaded = Assert.Single(await db.GetAllBooksAsync());

        Assert.Equal(book.Id, loaded.Id);
        Assert.Equal("Maus", loaded.Title);
        Assert.Equal("Jeff Smith", loaded.Author);
        Assert.Equal("1991", loaded.Year);
        Assert.Equal("Cartoon Books", loaded.Publisher);
        Assert.Equal(book.Isbn, loaded.Isbn);
        Assert.Equal(book.FilePath, loaded.FilePath);
        Assert.Equal(book.CoverImagePath, loaded.CoverImagePath);
    }

    [Fact]
    public async Task AddBookAsync_MultipleBooks_AllReturnedWithDistinctIds()
    {
        var db = NewDatabase();
        var a = MakeBook("A");
        var b = MakeBook("B");
        var c = MakeBook("C");

        await db.AddBookAsync(a);
        await db.AddBookAsync(b);
        await db.AddBookAsync(c);

        var all = await db.GetAllBooksAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal(3, all.Select(x => x.Id).Distinct().Count());
        Assert.Equal(new[] { "A", "B", "C" }, all.OrderBy(x => x.Id).Select(x => x.Title));
    }

    // ---------------------------------------------------------------------
    //  Update
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateBookAsync_ChangesPersistedFields_AndReturnsOne()
    {
        var db = NewDatabase();
        var book = MakeBook("Draft Title");
        await db.AddBookAsync(book);

        book.Title = "Final Title";
        book.Year = "1992";
        int rows = await db.UpdateBookAsync(book);

        Assert.Equal(1, rows);
        var loaded = Assert.Single(await db.GetAllBooksAsync());
        Assert.Equal("Final Title", loaded.Title);
        Assert.Equal("1992", loaded.Year);
    }

    [Fact]
    public async Task UpdateBookAsync_ForBookNotInDatabase_ReturnsZero()
    {
        var db = NewDatabase();
        var stranger = MakeBook("Never Inserted");
        stranger.Id = 4242;

        int rows = await db.UpdateBookAsync(stranger);

        Assert.Equal(0, rows);
    }

    // ---------------------------------------------------------------------
    //  Delete
    // ---------------------------------------------------------------------

    [Fact]
    public async Task DeleteBookAsync_RemovesRow_AndReturnsOne()
    {
        var db = NewDatabase();
        var book = MakeBook();
        await db.AddBookAsync(book);

        int rows = await db.DeleteBookAsync(book);

        Assert.Equal(1, rows);
        Assert.Empty(await db.GetAllBooksAsync());
    }

    [Fact]
    public async Task DeleteBookAsync_ForBookNotInDatabase_ReturnsZero()
    {
        var db = NewDatabase();
        var stranger = MakeBook();
        stranger.Id = 999;

        int rows = await db.DeleteBookAsync(stranger);

        Assert.Equal(0, rows);
    }

    [Fact]
    public async Task DeleteBookAsync_OnlyRemovesTheTargetedBook()
    {
        var db = NewDatabase();
        var keep = MakeBook("Keep");
        var drop = MakeBook("Drop");
        await db.AddBookAsync(keep);
        await db.AddBookAsync(drop);

        await db.DeleteBookAsync(drop);

        var remaining = Assert.Single(await db.GetAllBooksAsync());
        Assert.Equal("Keep", remaining.Title);
    }

    // ---------------------------------------------------------------------
    //  Persistence
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Data_PersistsAcrossConnectionsToTheSameFile()
    {
        string path = NewPath();

        var writer = new BookDatabase(path);
        await writer.AddBookAsync(MakeBook("Persisted"));

        var reader = new BookDatabase(path);
        var loaded = Assert.Single(await reader.GetAllBooksAsync());
        Assert.Equal("Persisted", loaded.Title);
    }
}
