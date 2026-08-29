namespace ComicReaderApp;

/// <summary>
/// Builds an <see cref="IComicSource"/> for a book file. This is the single place where a file
/// extension is mapped to a concrete reader (currently only <c>.pdf</c>); add new formats here.
/// Abstracted so <see cref="MainPageViewModel"/> can be tested with a fake source.
/// </summary>
public interface IComicSourceFactory
{
    /// <summary>Loads a comic source for <paramref name="filePath"/>, or throws
    /// <see cref="NotSupportedException"/> for an unrecognised extension.</summary>
    Task<IComicSource> CreateAsync(string filePath);
}
