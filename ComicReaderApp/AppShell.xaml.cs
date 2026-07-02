namespace ComicReaderApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(LibraryPage), typeof(LibraryPage));
    }
}