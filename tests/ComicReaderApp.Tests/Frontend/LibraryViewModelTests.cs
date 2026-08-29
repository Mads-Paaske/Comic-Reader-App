using System.ComponentModel;
using ComicReaderApp;
using ComicReaderApp.Tests.Shared;
using Xunit;

namespace ComicReaderApp.Tests.Frontend;

/// <summary>
/// Tests for the Library page view model (<see cref="LibraryViewModel"/>) — the state and
/// persistence behaviour behind the library screen. Navigation commands that reach into the
/// static <c>Shell.Current</c> (OpenBook / AddBook / LongPress) are not covered here; they
/// need a navigation abstraction first.
/// </summary>
public class LibraryViewModelTests
{
    static Book MakeBook(string title = "Untitled", string? path = null) => new()
    {
        Title = title,
        Author = "Some Author",
        Year = "2024",
        FilePath = path ?? $@"C:\comics\{title}.pdf",
    };

    /// <summary>Records every PropertyChanged name raised by a source.</summary>
    static List<string> RecordPropertyChanges(INotifyPropertyChanged source)
    {
        var raised = new List<string>();
        source.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? "");
        return raised;
    }

    // ---------------------------------------------------------------------
    //  Initial state
    // ---------------------------------------------------------------------

    [Fact]
    public void NewViewModel_StartsEmpty()
    {
        var vm = new LibraryViewModel(new FakeBookRepository());

        Assert.Empty(vm.Books);
        Assert.True(vm.IsLibraryEmpty);
    }

    [Fact]
    public void NewViewModel_ExposesAllCommands()
    {
        var vm = new LibraryViewModel(new FakeBookRepository());

        Assert.NotNull(vm.OpenBookCommand);
        Assert.NotNull(vm.AddBookCommand);
        Assert.NotNull(vm.OpenSettingsCommand);
        Assert.NotNull(vm.LongPressBookCommand);
    }

    [Fact]
    public void ItemPadding_IsDerivedFromHorizontalPadding()
    {
        var vm = new LibraryViewModel(new FakeBookRepository());

        Assert.Equal(vm.HorizontalPadding, vm.ItemPadding.Left);
        Assert.Equal(vm.HorizontalPadding, vm.ItemPadding.Right);
        Assert.Equal(10, vm.ItemPadding.Top);
        Assert.Equal(10, vm.ItemPadding.Bottom);
    }

    // ---------------------------------------------------------------------
    //  LoadBooksAsync — success cases
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LoadBooksAsync_PopulatesBooksFromRepository()
    {
        var repo = new FakeBookRepository(MakeBook("Bone"), MakeBook("Maus"), MakeBook("Watchmen"));
        var vm = new LibraryViewModel(repo);

        await vm.LoadBooksAsync();

        Assert.Equal(3, vm.Books.Count);
        Assert.Equal(new[] { "Bone", "Maus", "Watchmen" }, vm.Books.Select(b => b.Title));
        Assert.False(vm.IsLibraryEmpty);
    }

    [Fact]
    public async Task LoadBooksAsync_WithEmptyRepository_LeavesLibraryEmpty()
    {
        var vm = new LibraryViewModel(new FakeBookRepository());

        await vm.LoadBooksAsync();

        Assert.Empty(vm.Books);
        Assert.True(vm.IsLibraryEmpty);
    }

    [Fact]
    public async Task LoadBooksAsync_CalledTwice_DoesNotDuplicateEntries()
    {
        var repo = new FakeBookRepository(MakeBook("Persepolis"), MakeBook("Sandman"));
        var vm = new LibraryViewModel(repo);

        await vm.LoadBooksAsync();
        await vm.LoadBooksAsync();

        Assert.Equal(2, vm.Books.Count);
    }

    [Fact]
    public async Task LoadBooksAsync_RaisesIsLibraryEmpty_WhenBooksAppear()
    {
        var repo = new FakeBookRepository(MakeBook("Akira"));
        var vm = new LibraryViewModel(repo);
        var changes = RecordPropertyChanges(vm);

        await vm.LoadBooksAsync();

        Assert.Contains(nameof(LibraryViewModel.IsLibraryEmpty), changes);
    }

    // ---------------------------------------------------------------------
    //  LoadBooksAsync — failure cases
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LoadBooksAsync_WhenRepositoryThrows_PropagatesException()
    {
        var repo = new FakeBookRepository { ThrowOnGetAll = true };
        var vm = new LibraryViewModel(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.LoadBooksAsync());
    }

    [Fact]
    public async Task LoadBooksAsync_WhenRepositoryThrows_KeepsPreviouslyLoadedBooks()
    {
        var repo = new FakeBookRepository(MakeBook("Blame"), MakeBook("Nausicaa"));
        var vm = new LibraryViewModel(repo);
        await vm.LoadBooksAsync();

        repo.ThrowOnGetAll = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.LoadBooksAsync());

        Assert.Equal(2, vm.Books.Count);
    }

    // ---------------------------------------------------------------------
    //  AddBook
    // ---------------------------------------------------------------------

    [Fact]
    public async Task AddBook_PersistsThroughRepository()
    {
        var repo = new FakeBookRepository();
        var vm = new LibraryViewModel(repo);

        await vm.AddBook(MakeBook("Chainsaw Man"));

        Assert.Equal(1, repo.AddCallCount);
        Assert.Single(repo.Snapshot);
        Assert.Equal("Chainsaw Man", repo.Snapshot[0].Title);
    }

    [Fact]
    public async Task AddBook_AssignsAnId()
    {
        var repo = new FakeBookRepository();
        var vm = new LibraryViewModel(repo);
        var book = MakeBook("Vinland Saga");

        await vm.AddBook(book);

        Assert.True(book.Id > 0);
    }

    [Fact]
    public async Task AddBook_ThenReload_ShowsTheNewBook()
    {
        var repo = new FakeBookRepository();
        var vm = new LibraryViewModel(repo);

        await vm.AddBook(MakeBook("Berserk"));
        await vm.LoadBooksAsync();

        Assert.Single(vm.Books);
        Assert.Equal("Berserk", vm.Books[0].Title);
    }

    [Fact]
    public async Task AddBook_WhenRepositoryThrows_PropagatesAndPersistsNothing()
    {
        var repo = new FakeBookRepository { ThrowOnAdd = true };
        var vm = new LibraryViewModel(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.AddBook(MakeBook("Uzumaki")));
        Assert.Empty(repo.Snapshot);
    }

    // ---------------------------------------------------------------------
    //  UpdateBook
    // ---------------------------------------------------------------------

    [Fact]
    public async Task UpdateBook_PersistsChangedFields()
    {
        var book = MakeBook("Orignal Titel");
        var repo = new FakeBookRepository(book);
        var vm = new LibraryViewModel(repo);

        book.Title = "Corrected Title";
        await vm.UpdateBook(book);

        Assert.Equal(1, repo.UpdateCallCount);
        Assert.Equal("Corrected Title", repo.Snapshot.Single().Title);
    }

    [Fact]
    public async Task UpdateBook_ForUnknownBook_AffectsNothing()
    {
        var repo = new FakeBookRepository(MakeBook("Only Book") /* Id 1 */);
        var vm = new LibraryViewModel(repo);
        var stranger = MakeBook("Not In Library");
        stranger.Id = 999;

        await vm.UpdateBook(stranger);

        Assert.Equal("Only Book", repo.Snapshot.Single().Title);
    }

    [Fact]
    public async Task UpdateBook_WhenRepositoryThrows_PropagatesException()
    {
        var book = MakeBook("Goodnight Punpun");
        var repo = new FakeBookRepository(book) { ThrowOnUpdate = true };
        var vm = new LibraryViewModel(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vm.UpdateBook(book));
    }

    // ---------------------------------------------------------------------
    //  IsLibraryEmpty / SlotWidth notifications
    // ---------------------------------------------------------------------

    [Fact]
    public void AddingToBooks_MakesLibraryNonEmpty_AndNotifies()
    {
        var vm = new LibraryViewModel(new FakeBookRepository());
        var changes = RecordPropertyChanges(vm);

        vm.Books.Add(MakeBook());

        Assert.False(vm.IsLibraryEmpty);
        Assert.Contains(nameof(LibraryViewModel.IsLibraryEmpty), changes);
    }

    [Fact]
    public void RemovingLastBook_MakesLibraryEmptyAgain()
    {
        var vm = new LibraryViewModel(new FakeBookRepository());
        var book = MakeBook();
        vm.Books.Add(book);

        vm.Books.Remove(book);

        Assert.True(vm.IsLibraryEmpty);
    }

    [Fact]
    public void SlotWidth_RaisesPropertyChanged_WhenSet()
    {
        var vm = new LibraryViewModel(new FakeBookRepository());
        var changes = RecordPropertyChanges(vm);

        vm.SlotWidth = 137;

        Assert.Equal(137, vm.SlotWidth);
        Assert.Contains(nameof(LibraryViewModel.SlotWidth), changes);
    }
}
