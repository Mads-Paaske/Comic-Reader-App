namespace ComicReaderApp;

/// <summary>
/// Persistence contract for the library. Abstracts <see cref="BookDatabase"/> so view models
/// can be unit-tested against an in-memory fake instead of a real SQLite connection.
/// </summary>
public interface IBookRepository
{
    Task<List<Book>> GetAllBooksAsync();
    Task<int> AddBookAsync(Book book);
    Task<int> UpdateBookAsync(Book book);
    Task<int> DeleteBookAsync(Book book);
}
