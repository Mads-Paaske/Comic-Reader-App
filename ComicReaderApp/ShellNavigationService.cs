namespace ComicReaderApp;

/// <summary>Production <see cref="INavigationService"/> that forwards to <see cref="Shell.Current"/>.</summary>
public class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);

    public Task GoToAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync(route, parameters);

    public Task DisplayAlert(string title, string message, string cancel) =>
        Shell.Current.DisplayAlert(title, message, cancel);

    public Task<bool> DisplayConfirmation(string title, string message, string accept, string cancel) =>
        Shell.Current.DisplayAlert(title, message, accept, cancel);

    public Task<string> DisplayActionSheet(string title, string cancel, string? destruction, params string[] buttons) =>
        Shell.Current.DisplayActionSheet(title, cancel, destruction, buttons);
}
