using System.ComponentModel;
using ComicReaderApp;
using ComicReaderApp.Tests.Shared;
using Xunit;

namespace ComicReaderApp.Tests.Frontend;

/// <summary>
/// Tests for the Add/Edit Book page view model (<see cref="AddBookViewModel"/>): mode switching,
/// field population, file picking, and the save-time validation rules plus the two success paths
/// (new book vs. edited book) that navigate back with a parameter payload.
/// </summary>
public class AddBookViewModelTests
{
    readonly FakeNavigationService _nav = new();
    readonly FakeComicFilePicker _picker = new();

    AddBookViewModel MakeVm() => new(_nav, _picker);

    static string CreateTempFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"crtest_{Guid.NewGuid():N}.pdf");
        File.WriteAllText(path, "not really a pdf");
        return path;
    }

    static List<string> RecordPropertyChanges(INotifyPropertyChanged source)
    {
        var raised = new List<string>();
        source.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? "");
        return raised;
    }

    // ---------------------------------------------------------------------
    //  Mode / initial state
    // ---------------------------------------------------------------------

    [Fact]
    public void NewViewModel_IsInAddMode()
    {
        var vm = MakeVm();

        Assert.Equal("Add Book", vm.PageTitle);
        Assert.Equal("Save", vm.SaveButtonText);
        Assert.Equal("Choose File...", vm.PickedFileName);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void LoadForEdit_CopiesFieldsAndEntersEditMode()
    {
        var vm = MakeVm();
        var book = new Book
        {
            Title = "Watchmen", Author = "Alan Moore", Year = "1987",
            Publisher = "DC", Isbn = "9781401245252", FilePath = @"C:\comics\watchmen.pdf",
        };

        vm.LoadForEdit(book);

        Assert.Equal("Watchmen", vm.Title);
        Assert.Equal("Alan Moore", vm.Author);
        Assert.Equal("1987", vm.Year);
        Assert.Equal("DC", vm.Publisher);
        Assert.Equal("9781401245252", vm.Isbn);
        Assert.Equal("watchmen.pdf", vm.PickedFileName);
        Assert.Equal("Edit Book", vm.PageTitle);
        Assert.Equal("Update", vm.SaveButtonText);
    }

    [Fact]
    public void LoadForEdit_WithBlankFilePath_ShowsChoosePrompt()
    {
        var vm = MakeVm();

        vm.LoadForEdit(new Book { Title = "No File", FilePath = "" });

        Assert.Equal("Choose File...", vm.PickedFileName);
    }

    [Fact]
    public void LoadForEdit_RaisesPageTitleAndSaveButtonText()
    {
        var vm = MakeVm();
        var changes = RecordPropertyChanges(vm);

        vm.LoadForEdit(new Book { Title = "X", FilePath = @"C:\x.pdf" });

        Assert.Contains(nameof(AddBookViewModel.PageTitle), changes);
        Assert.Contains(nameof(AddBookViewModel.SaveButtonText), changes);
    }

    // ---------------------------------------------------------------------
    //  ErrorMessage / HasError
    // ---------------------------------------------------------------------

    [Fact]
    public void ErrorMessage_Set_TogglesHasError_AndNotifiesBoth()
    {
        var vm = MakeVm();
        var changes = RecordPropertyChanges(vm);

        vm.ErrorMessage = "Something went wrong";

        Assert.True(vm.HasError);
        Assert.Contains(nameof(AddBookViewModel.ErrorMessage), changes);
        Assert.Contains(nameof(AddBookViewModel.HasError), changes);
    }

    [Fact]
    public void ErrorMessage_Cleared_ResetsHasError()
    {
        var vm = MakeVm();
        vm.ErrorMessage = "boom";

        vm.ErrorMessage = "";

        Assert.False(vm.HasError);
    }

    // ---------------------------------------------------------------------
    //  PickFileCommand
    // ---------------------------------------------------------------------

    [Fact]
    public void PickFile_WhenUserPicksFile_UpdatesFileName()
    {
        var vm = MakeVm();
        _picker.NextResult = new PickedFile(@"C:\comics\my.pdf", "my.pdf");

        vm.PickFileCommand.Execute(null);

        Assert.Equal("my.pdf", vm.PickedFileName);
        Assert.Equal(1, _picker.CallCount);
    }

    [Fact]
    public void PickFile_WhenUserCancels_LeavesFileNameUnchanged()
    {
        var vm = MakeVm();
        _picker.NextResult = null;

        vm.PickFileCommand.Execute(null);

        Assert.Equal("Choose File...", vm.PickedFileName);
    }

    [Fact]
    public void PickFile_WhenPickerThrows_SetsError()
    {
        var vm = MakeVm();
        _picker.PickException = new InvalidOperationException("access denied");

        vm.PickFileCommand.Execute(null);

        Assert.True(vm.HasError);
        Assert.Contains("access denied", vm.ErrorMessage);
    }

    // ---------------------------------------------------------------------
    //  SaveCommand — validation failures
    // ---------------------------------------------------------------------

    [Fact]
    public void Save_WithBlankTitle_SetsError_AndDoesNotNavigate()
    {
        var vm = MakeVm();

        vm.SaveCommand.Execute(null);

        Assert.Equal("Title is required.", vm.ErrorMessage);
        Assert.Empty(_nav.Navigations);
    }

    [Fact]
    public void Save_WithTitleButNoFile_SetsError()
    {
        var vm = MakeVm();
        vm.Title = "Has a title";

        vm.SaveCommand.Execute(null);

        Assert.Equal("Please choose a file.", vm.ErrorMessage);
        Assert.Empty(_nav.Navigations);
    }

    [Fact]
    public void Save_InEditMode_WithMissingFile_SetsError_AndResetsPickedFile()
    {
        var vm = MakeVm();
        vm.LoadForEdit(new Book
        {
            Title = "Ghost", FilePath = Path.Combine(Path.GetTempPath(), "does-not-exist-12345.pdf"),
        });

        vm.SaveCommand.Execute(null);

        Assert.StartsWith("That file can no longer be found", vm.ErrorMessage);
        Assert.Equal("Choose File...", vm.PickedFileName);
        Assert.Empty(_nav.Navigations);
    }

    // ---------------------------------------------------------------------
    //  SaveCommand — success paths
    // ---------------------------------------------------------------------

    [Fact]
    public void Save_InAddMode_WithPickedValidFile_NavigatesBackWithNewBook()
    {
        var path = CreateTempFile();
        try
        {
            var vm = MakeVm();
            _picker.NextResult = new PickedFile(path, Path.GetFileName(path));
            vm.PickFileCommand.Execute(null);

            vm.Title = "My Comic";
            vm.Author = "Me";
            vm.Year = "2020";

            vm.SaveCommand.Execute(null);

            Assert.Equal("..", _nav.LastNavigation?.Route);
            var newBook = Assert.IsType<Book>(_nav.LastNavigation?.Parameters?["NewBook"]);
            Assert.Equal("My Comic", newBook.Title);
            Assert.Equal("Me", newBook.Author);
            Assert.Equal("2020", newBook.Year);
            Assert.Equal(path, newBook.FilePath);
            Assert.False(vm.HasError);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Save_InEditMode_WithValidFile_NavigatesBackWithSameBookInstanceUpdated()
    {
        var path = CreateTempFile();
        try
        {
            var vm = MakeVm();
            var book = new Book { Title = "Old Title", Author = "Old Author", FilePath = path };
            vm.LoadForEdit(book);

            vm.Title = "New Title";
            vm.Author = "New Author";

            vm.SaveCommand.Execute(null);

            Assert.Equal("..", _nav.LastNavigation?.Route);
            Assert.Same(book, _nav.LastNavigation?.Parameters?["UpdatedBook"]);
            Assert.Equal("New Title", book.Title);
            Assert.Equal("New Author", book.Author);
            Assert.Equal(path, book.FilePath);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
