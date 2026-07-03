namespace ComicReaderApp;

public partial class MainPage : ContentPage, IQueryAttributable
{

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLibraryButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LibraryPage));
    }
    
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("SelectedBook", out var value) && value is Book book)
        {
            // for now, just prove it works:
            DisplayAlert("Opened", book.Title, "OK");
            // later: load book.FilePath into whatever renders the comic
        }
    }
}