using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ComicReaderApp;

public class MainPageViewModel : INotifyPropertyChanged
{
    readonly IComicSourceFactory _comicSourceFactory;

    IComicSource _comicSource;
    int _currentPageIndex;
    ImageSource _currentPageImage;
    string _pageIndicator;

    public ImageSource CurrentPageImage
    {
        get => _currentPageImage;
        set { _currentPageImage = value; OnPropertyChanged(); }
    }

    public string PageIndicator
    {
        get => _pageIndicator;
        set { _pageIndicator = value; OnPropertyChanged(); }
    }

    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }

    public MainPageViewModel(IComicSourceFactory comicSourceFactory)
    {
        _comicSourceFactory = comicSourceFactory;

        NextPageCommand = new Command(async () => await ChangePage(1), () => CanGoNext());
        PreviousPageCommand = new Command(async () => await ChangePage(-1), () => CanGoPrevious());
    }

    public async Task LoadBookAsync(Book book)
    {
        _comicSource = await _comicSourceFactory.CreateAsync(book.FilePath);

        _currentPageIndex = 0;
        await ShowCurrentPage();
    }

    async Task ChangePage(int direction)
    {
        _currentPageIndex += direction;
        await ShowCurrentPage();
    }

    async Task ShowCurrentPage()
    {
        CurrentPageImage = await _comicSource.GetPageAsync(_currentPageIndex);
        PageIndicator = $"{_currentPageIndex + 1} / {_comicSource.PageCount}";
        ((Command)NextPageCommand).ChangeCanExecute();
        ((Command)PreviousPageCommand).ChangeCanExecute();
    }

    bool CanGoNext() => _comicSource != null && _currentPageIndex < _comicSource.PageCount - 1;
    bool CanGoPrevious() => _comicSource != null && _currentPageIndex > 0;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}