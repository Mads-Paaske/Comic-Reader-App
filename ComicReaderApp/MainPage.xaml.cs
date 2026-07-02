namespace ComicReaderApp;

public partial class MainPage : ContentPage
{

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnLibraryButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LibraryPage));
    }
}