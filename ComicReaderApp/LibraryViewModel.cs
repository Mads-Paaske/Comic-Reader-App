using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ComicReaderApp;

public class LibraryViewModel : INotifyPropertyChanged
{
    
    double _slotWidth = 100;
    
    readonly IBookRepository _database;

    public bool IsLibraryEmpty => Books.Count == 0;

    public LibraryViewModel(IBookRepository database)
    {
        _database = database;
        
        OpenBookCommand = new Command<Book>(OnOpenBook);
        AddBookCommand = new Command(OnAddBook);
        OpenSettingsCommand = new Command(OnOpenSettings);
        LongPressBookCommand = new Command<Book>(OnLongPressBook);
        
        Books.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsLibraryEmpty));
    }

    
    public double HorizontalPadding { get; } = 5;

    public Thickness ItemPadding => new Thickness(HorizontalPadding, 10);
    
    
    public double SlotWidth
    {
        get => _slotWidth;
        set { _slotWidth = value; OnPropertyChanged(); }
    }
    
    public ObservableCollection<Book> Books { get; set; } = new();

    public ICommand OpenBookCommand { get; }
    public ICommand AddBookCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand LongPressBookCommand { get; }
    
    public async Task LoadBooksAsync()
    {
        var books = await _database.GetAllBooksAsync();
        Books.Clear();
        foreach (var book in books)
            Books.Add(book);
    }

    async void OnOpenBook(Book book)
    {
        if (string.IsNullOrEmpty(book.FilePath) || !File.Exists(book.FilePath))
        {
            await Shell.Current.DisplayAlert(
                "Can't Open Book",
                $"The file for \"{book.Title}\" couldn't be found. It may have been moved or deleted.",
                "OK");
            return;
        }

        try
        {
            var parameters = new Dictionary<string, object> { ["SelectedBook"] = book };
            await Shell.Current.GoToAsync("//MainPage", parameters);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Navigation Failed", ex.Message, "OK");
        }
    }
    
    void OnAddBook()
    {
        Shell.Current.GoToAsync(nameof(AddBookPage));
    }
    
    public async Task AddBook(Book book)
    {
        await _database.AddBookAsync(book);   // Id gets assigned here
    }
    
    void OnOpenSettings() { /* navigate to settings */ }

    public event PropertyChangedEventHandler PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    async void OnLongPressBook(Book book)
    {
        string action = await Shell.Current.DisplayActionSheet(
            book.Title, "Cancel", "Delete", "Edit");

        if (action == "Delete")
        {
            bool confirmed = await Shell.Current.DisplayAlert(
                "Delete Book",
                $"Remove \"{book.Title}\" from your library? This won't delete the original file.",
                "Delete", "Cancel");

            if (confirmed)
            {
                await _database.DeleteBookAsync(book);
                Books.Remove(book);
            }
        }
        else if (action == "Edit")
        {
            var parameters = new Dictionary<string, object> { ["EditBook"] = book };
            await Shell.Current.GoToAsync(nameof(AddBookPage), parameters);
        }
    }
    
    public async Task UpdateBook(Book book)
    {
        await _database.UpdateBookAsync(book);
    }
    
}