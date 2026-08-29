using SQLite;

namespace ComicReaderApp;

public class BookDatabase : IBookRepository
{
    readonly SQLiteAsyncConnection _connection;

    /// <summary>Production constructor: stores the DB in the app data directory.</summary>
    public BookDatabase()
        : this(Path.Combine(FileSystem.AppDataDirectory, "comicreader.db3"))
    {
    }

    /// <summary>Opens (or creates) the database at an explicit path. Used by tests.</summary>
    public BookDatabase(string dbPath)
    {
        _connection = new SQLiteAsyncConnection(dbPath);
        _connection.CreateTableAsync<Book>().Wait();
    }

    public Task<List<Book>> GetAllBooksAsync() => _connection.Table<Book>().ToListAsync();

    public Task<int> AddBookAsync(Book book) => _connection.InsertAsync(book);

    public Task<int> UpdateBookAsync(Book book) => _connection.UpdateAsync(book);

    public Task<int> DeleteBookAsync(Book book) => _connection.DeleteAsync(book);
}