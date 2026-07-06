using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ComicReaderApp;

public class AddBookViewModel : INotifyPropertyChanged
{
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

    public ICommand PickFileCommand { get; }
    public ICommand SaveCommand { get; }

    public AddBookViewModel()
    {
        PickFileCommand = new Command(OnPickFile);
        SaveCommand = new Command(OnSave);
    }

    async void OnPickFile()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a comic file"
            });

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

        // Layer 1: fast pre-check, since time has passed since the file was picked.
        if (!File.Exists(_pickedFilePath))
        {
            ErrorMessage = "That file can no longer be found. Please choose it again.";
            _pickedFilePath = null;
            PickedFileName = "Choose File...";
            return;
        }

        try
        {
            // Layer 2: the real safety net. A cheap Exists check can't catch
            // permission errors, a file becoming locked by another process,
            // or the file vanishing in the instant between the check above
            // and this actually running.
            using var testStream = File.OpenRead(_pickedFilePath);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't access that file: {ex.Message}";
            return;
        }

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
        await Shell.Current.GoToAsync("..", parameters);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}