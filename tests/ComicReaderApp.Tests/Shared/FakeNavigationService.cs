using ComicReaderApp;

namespace ComicReaderApp.Tests.Shared;

/// <summary>
/// Records navigation / dialog calls and returns canned answers. All returned tasks are already
/// completed, so <c>async void</c> command handlers run to completion synchronously in tests.
/// </summary>
public sealed class FakeNavigationService : INavigationService
{
    public record NavCall(string Route, IReadOnlyDictionary<string, object>? Parameters);

    public List<NavCall> Navigations { get; } = new();
    public List<string> AlertMessages { get; } = new();
    public List<string> ActionSheetTitles { get; } = new();

    /// <summary>Answer returned by <see cref="DisplayConfirmation"/> (the "accept" button).</summary>
    public bool ConfirmationResult { get; set; } = true;

    /// <summary>Answer returned by <see cref="DisplayActionSheet"/> (which button the user "tapped").</summary>
    public string? ActionSheetResult { get; set; }

    /// <summary>When set, the next <see cref="GoToAsync(string)"/> / GoToAsync(route, params) throws it.</summary>
    public Exception? GoToException { get; set; }

    public NavCall? LastNavigation => Navigations.Count > 0 ? Navigations[^1] : null;

    public Task GoToAsync(string route)
    {
        if (GoToException is not null) throw GoToException;
        Navigations.Add(new NavCall(route, null));
        return Task.CompletedTask;
    }

    public Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        if (GoToException is not null) throw GoToException;
        Navigations.Add(new NavCall(route, new Dictionary<string, object>(parameters)));
        return Task.CompletedTask;
    }

    public Task DisplayAlert(string title, string message, string cancel)
    {
        AlertMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task<bool> DisplayConfirmation(string title, string message, string accept, string cancel)
    {
        AlertMessages.Add(message);
        return Task.FromResult(ConfirmationResult);
    }

    public Task<string> DisplayActionSheet(string title, string cancel, string? destruction, params string[] buttons)
    {
        ActionSheetTitles.Add(title);
        return Task.FromResult(ActionSheetResult ?? cancel);
    }
}
