namespace ComicReaderApp;

/// <summary>
/// Wraps the <see cref="Shell"/> navigation and dialog calls the view models make, so they can be
/// unit-tested without a running MAUI shell. The production implementation is
/// <see cref="ShellNavigationService"/>.
/// </summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoToAsync(string route, IDictionary<string, object> parameters);

    /// <summary>Informational alert with a single dismiss button.</summary>
    Task DisplayAlert(string title, string message, string cancel);

    /// <summary>Confirmation alert; returns true when the user picks <paramref name="accept"/>.</summary>
    Task<bool> DisplayConfirmation(string title, string message, string accept, string cancel);

    Task<string> DisplayActionSheet(string title, string cancel, string? destruction, params string[] buttons);
}
