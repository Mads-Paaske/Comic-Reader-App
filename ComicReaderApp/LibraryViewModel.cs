using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ComicReaderApp;

public class LibraryViewModel : INotifyPropertyChanged
{
    
    double _slotWidth = 100;
    
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

    public LibraryViewModel()
    {
        OpenBookCommand = new Command<Book>(OnOpenBook);
        AddBookCommand = new Command(OnAddBook);
        OpenSettingsCommand = new Command(OnOpenSettings);

        // temp test data
        foreach (var i in Enumerable.Range(1, 23))
        {
            Books.Add(new Book { Title = $"Book {i}" });
        }
    }

    void OnOpenBook(Book book) { /* navigate to reader */ }
    void OnAddBook() { /* file picker */ }
    void OnOpenSettings() { /* navigate to settings */ }

    public event PropertyChangedEventHandler PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}