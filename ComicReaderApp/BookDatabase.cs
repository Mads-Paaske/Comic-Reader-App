using SQLite;

namespace ComicReaderApp;

public class BookDatabase
{
    readonly SQLiteAsyncConnection _connection;

    public BookDatabase()
    {
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "comicreader.db3");
        _connection = new SQLiteAsyncConnection(dbPath);
        _connection.CreateTableAsync<Book>().Wait();
    }

    public Task<List<Book>> GetAllBooksAsync() => _connection.Table<Book>().ToListAsync();

    public Task<int> AddBookAsync(Book book) => _connection.InsertAsync(book);

    public Task<int> UpdateBookAsync(Book book) => _connection.UpdateAsync(book);

    public Task<int> DeleteBookAsync(Book book) => _connection.DeleteAsync(book);
}