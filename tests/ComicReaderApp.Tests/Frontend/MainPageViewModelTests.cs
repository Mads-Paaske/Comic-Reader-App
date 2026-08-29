using System.ComponentModel;
using ComicReaderApp;
using ComicReaderApp.Tests.Shared;
using Xunit;

namespace ComicReaderApp.Tests.Frontend;

/// <summary>
/// Tests for the reader view model (<see cref="MainPageViewModel"/>): loading a book through the
/// <see cref="IComicSourceFactory"/> seam, the page indicator, and the next/previous command
/// enable-state at the start, middle, and end of a comic.
/// </summary>
public class MainPageViewModelTests
{
    static Book AnyBook() => new() { Title = "Test", FilePath = @"C:\comics\test.pdf" };

    static List<string> RecordPropertyChanges(INotifyPropertyChanged source)
    {
        var raised = new List<string>();
        source.PropertyChanged += (_, e) => raised.Add(e.PropertyName ?? "");
        return raised;
    }

    // ---------------------------------------------------------------------
    //  Before a book is loaded
    // ---------------------------------------------------------------------

    [Fact]
    public void NewViewModel_NavigationCommandsDisabled()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory());

        Assert.False(vm.NextPageCommand.CanExecute(null));
        Assert.False(vm.PreviousPageCommand.CanExecute(null));
    }

    // ---------------------------------------------------------------------
    //  LoadBookAsync — success
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LoadBookAsync_ShowsFirstPageAndIndicator()
    {
        var factory = new FakeComicSourceFactory(pageCount: 3);
        var vm = new MainPageViewModel(factory);

        await vm.LoadBookAsync(AnyBook());

        Assert.Equal("1 / 3", vm.PageIndicator);
        Assert.Equal(new[] { 0 }, factory.LastCreated!.RequestedPages);   // first page was fetched
    }

    [Fact]
    public async Task LoadBookAsync_OnFirstPage_PreviousDisabled_NextEnabled()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory(pageCount: 3));

        await vm.LoadBookAsync(AnyBook());

        Assert.False(vm.PreviousPageCommand.CanExecute(null));
        Assert.True(vm.NextPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task LoadBookAsync_PassesTheBooksFilePathToTheFactory()
    {
        var factory = new FakeComicSourceFactory(pageCount: 2);
        var vm = new MainPageViewModel(factory);
        var book = new Book { Title = "X", FilePath = @"D:\stuff\book.pdf" };

        await vm.LoadBookAsync(book);

        Assert.Equal(@"D:\stuff\book.pdf", factory.LastRequestedPath);
    }

    [Fact]
    public async Task LoadBookAsync_RaisesCurrentPageImageAndPageIndicator()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory(pageCount: 3));
        var changes = RecordPropertyChanges(vm);

        await vm.LoadBookAsync(AnyBook());

        Assert.Contains(nameof(MainPageViewModel.CurrentPageImage), changes);
        Assert.Contains(nameof(MainPageViewModel.PageIndicator), changes);
    }

    [Fact]
    public async Task LoadBookAsync_SecondBook_ResetsToFirstPage()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory(pageCount: 3));
        await vm.LoadBookAsync(AnyBook());
        vm.NextPageCommand.Execute(null);
        vm.NextPageCommand.Execute(null);   // now on "3 / 3"

        await vm.LoadBookAsync(AnyBook());

        Assert.Equal("1 / 3", vm.PageIndicator);
    }

    // ---------------------------------------------------------------------
    //  Paging through a comic
    // ---------------------------------------------------------------------

    [Fact]
    public async Task NextPage_AdvancesIndicatorAndEnablesPrevious()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory(pageCount: 3));
        await vm.LoadBookAsync(AnyBook());

        vm.NextPageCommand.Execute(null);

        Assert.Equal("2 / 3", vm.PageIndicator);
        Assert.True(vm.PreviousPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task NextPage_AtLastPage_DisablesNext()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory(pageCount: 3));
        await vm.LoadBookAsync(AnyBook());

        vm.NextPageCommand.Execute(null);
        vm.NextPageCommand.Execute(null);

        Assert.Equal("3 / 3", vm.PageIndicator);
        Assert.False(vm.NextPageCommand.CanExecute(null));
        Assert.True(vm.PreviousPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task PreviousPage_GoesBackOnePage()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory(pageCount: 3));
        await vm.LoadBookAsync(AnyBook());
        vm.NextPageCommand.Execute(null);
        vm.NextPageCommand.Execute(null);

        vm.PreviousPageCommand.Execute(null);

        Assert.Equal("2 / 3", vm.PageIndicator);
    }

    [Fact]
    public async Task SinglePageComic_BothNavigationCommandsDisabled()
    {
        var vm = new MainPageViewModel(new FakeComicSourceFactory(pageCount: 1));

        await vm.LoadBookAsync(AnyBook());

        Assert.Equal("1 / 1", vm.PageIndicator);
        Assert.False(vm.NextPageCommand.CanExecute(null));
        Assert.False(vm.PreviousPageCommand.CanExecute(null));
    }

    [Fact]
    public async Task Paging_RequestsPagesInOrderFromTheSource()
    {
        var factory = new FakeComicSourceFactory(pageCount: 3);
        var vm = new MainPageViewModel(factory);
        await vm.LoadBookAsync(AnyBook());

        vm.NextPageCommand.Execute(null);
        vm.PreviousPageCommand.Execute(null);

        Assert.Equal(new[] { 0, 1, 0 }, factory.LastCreated!.RequestedPages);
    }

    // ---------------------------------------------------------------------
    //  LoadBookAsync — failure
    // ---------------------------------------------------------------------

    [Fact]
    public async Task LoadBookAsync_WhenFormatUnsupported_PropagatesNotSupported()
    {
        var factory = new FakeComicSourceFactory
        {
            CreateException = new NotSupportedException("Unsupported format: test.cbz"),
        };
        var vm = new MainPageViewModel(factory);

        await Assert.ThrowsAsync<NotSupportedException>(() => vm.LoadBookAsync(AnyBook()));
    }

    [Fact]
    public async Task LoadBookAsync_WhenSourceFailsToOpen_PropagatesException()
    {
        var factory = new FakeComicSourceFactory
        {
            CreateException = new IOException("file is locked"),
        };
        var vm = new MainPageViewModel(factory);

        await Assert.ThrowsAsync<IOException>(() => vm.LoadBookAsync(AnyBook()));
    }

    [Fact]
    public async Task LoadBookAsync_WhenLoadFails_NavigationCommandsStayDisabled()
    {
        var factory = new FakeComicSourceFactory
        {
            CreateException = new NotSupportedException("nope"),
        };
        var vm = new MainPageViewModel(factory);

        await Assert.ThrowsAsync<NotSupportedException>(() => vm.LoadBookAsync(AnyBook()));

        Assert.False(vm.NextPageCommand.CanExecute(null));
        Assert.False(vm.PreviousPageCommand.CanExecute(null));
    }
}
