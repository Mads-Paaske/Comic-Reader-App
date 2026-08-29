using ComicReaderApp;

namespace ComicReaderApp.Tests.Shared;

/// <summary>Test <see cref="IComicFilePicker"/>: returns <see cref="NextResult"/>, or throws
/// <see cref="PickException"/> to model a picker failure / permission denial.</summary>
public sealed class FakeComicFilePicker : IComicFilePicker
{
    public PickedFile? NextResult { get; set; }
    public Exception? PickException { get; set; }
    public int CallCount { get; private set; }

    public Task<PickedFile?> PickComicAsync()
    {
        CallCount++;
        if (PickException is not null) throw PickException;
        return Task.FromResult(NextResult);
    }
}
