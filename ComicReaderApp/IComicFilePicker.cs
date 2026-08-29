namespace ComicReaderApp;

/// <summary>A file the user picked: its full path and display name.</summary>
public record PickedFile(string FullPath, string FileName);

/// <summary>
/// Wraps the MAUI platform file picker so <see cref="AddBookViewModel"/> can be tested without it.
/// (Named to avoid a clash with <c>Microsoft.Maui.Storage.IFilePicker</c>.) Production
/// implementation: <see cref="ComicFilePicker"/>.
/// </summary>
public interface IComicFilePicker
{
    /// <summary>Prompts the user to pick a comic file; returns <c>null</c> if they cancel.</summary>
    Task<PickedFile?> PickComicAsync();
}
