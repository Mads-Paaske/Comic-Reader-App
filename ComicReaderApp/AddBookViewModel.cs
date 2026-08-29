using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ComicReaderApp;

public class AddBookViewModel : INotifyPropertyChanged
{
    readonly INavigationService _navigation;
    readonly IComicFilePicker _filePicker;

    Book _editingBook;

    string _title, _author, _year, _publisher, _isbn, _errorMessage;
    string _pickedFilePath;
    string _pickedFileName = "Choose File...";

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string Author { get => _author; set { _author = value; OnPropertyChanged(); } }
    public string Year { get => _year; set { _year = value; OnPropertyChanged(); } }
    public string Publisher { get => _publisher; set { _publisher = value; OnPropertyChanged(); } }
    public string Isbn { get => _isbn; set { _isbn = value; OnPropertyChanged(); } }
    public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public string PickedFileName { get => _pickedFileName; set { _pickedFileName = value; OnPropertyChanged(); } }

    public string PageTitle => _editingBook == null ? "Add Book" : "Edit Book";
    public string SaveButtonText => _editingBook == null ? "Save" : "Update";
    
    public ICommand PickFileCommand { get; }
    public ICommand SaveCommand { get; }

    public AddBookViewModel(INavigationService navigation, IComicFilePicker filePicker)
    {
        _navigation = navigation;
        _filePicker = filePicker;

        PickFileCommand = new Command(OnPickFile);
        SaveCommand = new Command(OnSave);
    }

    async void OnPickFile()
    {
        try
        {
            var result = await _filePicker.PickComicAsync();

            if (result != null)
            {
                _pickedFilePath = result.FullPath;
                PickedFileName = result.FileName;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"File pick failed: {ex.Message}";
        }
    }
    
    public void LoadForEdit(Book book)
    {
        _editingBook = book;
        Title = book.Title;
        Author = book.Author;
        Year = book.Year;
        Publisher = book.Publisher;
        Isbn = book.Isbn;
        _pickedFilePath = book.FilePath;
        PickedFileName = string.IsNullOrEmpty(book.FilePath)
            ? "Choose File..."
            : Path.GetFileName(book.FilePath);

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SaveButtonText));
    }   

    async void OnSave()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_pickedFilePath))
        {
            ErrorMessage = "Please choose a file.";
            return;
        }

        if (!File.Exists(_pickedFilePath))
        {
            ErrorMessage = "That file can no longer be found. Please choose it again.";
            _pickedFilePath = null;
            PickedFileName = "Choose File...";
            return;
        }

        try
        {
            using var testStream = File.OpenRead(_pickedFilePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't access that file: {ex.Message}";
            return;
        }

        if (_editingBook != null)
        {
            _editingBook.Title = Title;
            _editingBook.Author = Author;
            _editingBook.Year = Year;
            _editingBook.Publisher = Publisher;
            _editingBook.Isbn = Isbn;
            _editingBook.FilePath = _pickedFilePath;

            var parameters = new Dictionary<string, object> { ["UpdatedBook"] = _editingBook };
            await _navigation.GoToAsync("..", parameters);
        }
        else
        {
            var book = new Book
            {
                Title = Title,
                Author = Author,
                Year = Year,
                Publisher = Publisher,
                Isbn = Isbn,
                FilePath = _pickedFilePath
            };

            var parameters = new Dictionary<string, object> { ["NewBook"] = book };
            await _navigation.GoToAsync("..", parameters);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}