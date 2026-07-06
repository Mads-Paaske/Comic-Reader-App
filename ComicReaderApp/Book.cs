using SQLite;

namespace ComicReaderApp;

[Table("Books")]
public class Book
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; }
    public string Author { get; set; }
    public string Year { get; set; }
    public string Publisher { get; set; }
    public string Isbn { get; set; }
    public string FilePath { get; set; }
    public string CoverImagePath { get; set; }
}