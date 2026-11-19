namespace Nazor;

/// <summary>
/// A place in the history of a window: an address (<c>/orders/42</c>) or a page type with its parameters, for pages
/// reached without an address.
/// </summary>
/// <param name="Uri">The target address or image URI.</param>
/// <param name="Page">The page (Type?).</param>
/// <param name="Parameters">The parameters (IReadOnlyDictionary&lt;string, object?&gt;?).</param>
public sealed record NavigationEntry(string? Uri, Type? Page = null, IReadOnlyDictionary<string, object?>? Parameters = null);

/// <summary>The operation that made a navigation history entry current.</summary>
public enum NavigationKind
{
    /// <summary>Appends a new history entry.</summary>
    New,
    /// <summary>Replaces the current history entry.</summary>
    Replace,
    /// <summary>Returns to the preceding history entry.</summary>
    Back,
    /// <summary>Advances to the following history entry.</summary>
    Forward,
}

/// <summary>The location changed: <see cref="Entry" /> is the current one.</summary>
/// <param name="entry">The navigation entry that became current.</param>
/// <param name="kind">The operation that selected this entry.</param>
public sealed class LocationChangedEventArgs(NavigationEntry entry, NavigationKind kind = NavigationKind.New) : EventArgs
{
    /// <summary>Gets the navigation entry that became current.</summary>
    public NavigationEntry Entry { get; } = entry;

    /// <summary>Gets the current address, or null for navigation directly to a page type.</summary>
    public string? Uri => Entry.Uri;

    /// <summary>Gets the operation that selected the current entry, for directional page transitions.</summary>
    public NavigationKind Kind { get; } = kind;
}

/// <summary>A navigation about to happen, which a handler can stop (unsaved changes).</summary>
/// <param name="target">The component, element, or navigation entry targeted by the operation.</param>
/// <param name="current">The current navigation entry before the change.</param>
public sealed class LocationChangingContext(NavigationEntry target, NavigationEntry current)
{
    /// <summary>Gets the proposed destination of the pending navigation.</summary>
    public NavigationEntry Target { get; } = target;

    /// <summary>Gets the navigation entry being left.</summary>
    public NavigationEntry Current { get; } = current;

    /// <summary>Gets whether a location-changing handler canceled the pending navigation.</summary>
    public bool IsNavigationPrevented { get; private set; }

    /// <summary>Cancels the navigation currently being evaluated by location-changing handlers.</summary>
    public void PreventNavigation() => IsNavigationPrevented = true;
}

/// <summary>
/// The navigation of a window (one per DI scope, as the window): the current place, a history with back and forward,
/// and the handlers that can stop a navigation. A <c>Frame</c> shows the page of the current place. Methods run on
/// the UI thread.
/// </summary>
public sealed class NavigationManager
{
    private readonly List<NavigationEntry> history = [new("/")];
    private readonly List<LocationGuard> changing = [];
    private int index;
    private long requestVersion;

    /// <summary>The current place; at the start the address <c>/</c>.</summary>
    public NavigationEntry Current => history[index];

    /// <summary>The address of the current place, or null for a page reached by type.</summary>
    public string? Uri => Current.Uri;

    /// <summary>Gets whether a previous navigation history entry is available.</summary>
    public bool CanGoBack => index > 0;

    /// <summary>Gets whether a subsequent navigation history entry is available.</summary>
    public bool CanGoForward => index < history.Count - 1;

    /// <summary>Raised after the current place changed.</summary>
    public event EventHandler<LocationChangedEventArgs>? LocationChanged;

    /// <summary>Exceptions of the handlers of a navigation started without awaiting it.</summary>
    public event Action<Exception>? UnhandledException;

    /// <summary>
    /// Goes to <paramref name="uri" /> (absolute from the window, <c>/orders/42</c>); <paramref name="replace" />
    /// replaces the current place.
    /// </summary>
    /// <param name="uri">The target address or image URI.</param>
    /// <param name="replace">Whether to replace the current history entry instead of appending a new one.</param>
    public void NavigateTo(string uri, bool replace = false) => Observe(NavigateToAsync(uri, replace));

    /// <summary>Goes to a page by type, with its parameters, without an address.</summary>
    /// <param name="parameters">The parameter names and values to supply to the component.</param>
    /// <param name="replace">Whether to replace the current history entry instead of appending a new one.</param>
    /// <typeparam name="TPage">The page type.</typeparam>
    public void NavigateTo<TPage>(IReadOnlyDictionary<string, object?>? parameters = null, bool replace = false) where TPage : IComponent =>
        Observe(NavigateToAsync(new NavigationEntry(null, typeof(TPage), parameters), replace));

    /// <summary>
    /// Navigates to the previous history entry and reports asynchronous errors through UnhandledException.
    /// </summary>
    public void GoBack() => Observe(GoBackAsync());

    /// <summary>
    /// Navigates to the next history entry and reports asynchronous errors through UnhandledException.
    /// </summary>
    public void GoForward() => Observe(GoForwardAsync());

    /// <summary>Navigates to the supplied target after awaiting all location-changing handlers.</summary>
    /// <param name="uri">The target address or image URI.</param>
    /// <param name="replace">Whether to replace the current history entry instead of appending a new one.</param>
    /// <returns>A task returning true if navigation completed, or false if prevented or superseded by a newer request.</returns>
    public Task<bool> NavigateToAsync(string uri, bool replace = false) => NavigateToAsync(new NavigationEntry(Normalize(uri)), replace);

    /// <summary>Navigates to the supplied target after awaiting all location-changing handlers.</summary>
    /// <param name="target">The component, element, or navigation entry targeted by the operation.</param>
    /// <param name="replace">Whether to replace the current history entry instead of appending a new one.</param>
    /// <returns>A task returning true if navigation completed, or false if prevented or superseded by a newer request.</returns>
    /// <exception cref="ArgumentNullException">target is null.</exception>
    public async Task<bool> NavigateToAsync(NavigationEntry target, bool replace = false)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!await AllowAsync(target, ++requestVersion).ConfigureAwait(true)) return false;
        if (replace) history[index] = target;
        else
        {

            history.RemoveRange(index + 1, history.Count - index - 1);
            history.Add(target);
            index++;
        }
        Changed(replace ? NavigationKind.Replace : NavigationKind.New);
        return true;
    }

    /// <summary>Navigates to the previous history entry if one exists and no handler prevents the change.</summary>
    /// <returns>A task returning true if the previous entry became current, or false if unavailable or prevented.</returns>
    public Task<bool> GoBackAsync() => MoveAsync(-1);

    /// <summary>Navigates to the next history entry if one exists and no handler prevents the change.</summary>
    /// <returns>A task returning true if the next entry became current, or false if unavailable or prevented.</returns>
    public Task<bool> GoForwardAsync() => MoveAsync(1);

    /// <summary>
    /// Adds a handler that runs before every navigation and can stop it with
    /// <see cref="LocationChangingContext.PreventNavigation" />, also after awaiting (a confirmation); dispose the
    /// result to remove it (when the page goes).
    /// </summary>
    /// <param name="handler">The callback registered for the notification or command.</param>
    /// <returns>The resulting IDisposable value.</returns>
    /// <exception cref="ArgumentNullException">handler is null.</exception>
    public IDisposable RegisterLocationChangingHandler(Func<LocationChangingContext, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return RegisterHandler(handler, null);
    }

    /// <summary>
    /// Adds a page-scoped guard invoked only while the exact <paramref name="owner"/> history entry is current.
    /// Capture <see cref="Current"/> when the page is created. A retained outgoing page cannot veto later navigation;
    /// returning to that same history entry re-enables the guard until its registration is disposed.
    /// </summary>
    /// <param name="handler">The callback that can prevent leaving the owner entry.</param>
    /// <param name="owner">The exact history entry that owns the guard; matching uses reference identity.</param>
    /// <returns>An idempotent registration to dispose when the page is disposed.</returns>
    /// <exception cref="ArgumentNullException">The callback or owner is null.</exception>
    public IDisposable RegisterLocationChangingHandler(Func<LocationChangingContext, ValueTask> handler, NavigationEntry owner)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(owner);
        return RegisterHandler(handler, owner);
    }

    private IDisposable RegisterHandler(Func<LocationChangingContext, ValueTask> handler, NavigationEntry? owner)
    {
        var registration = new LocationGuard(handler, owner);
        changing.Add(registration);
        return new Registration(() => changing.Remove(registration));
    }

    private async Task<bool> MoveAsync(int step)
    {
        var target = index + step;
        if (target < 0 || target >= history.Count) return false;
        if (!await AllowAsync(history[target], ++requestVersion).ConfigureAwait(true)) return false;
        index = target;
        Changed(step < 0 ? NavigationKind.Back : NavigationKind.Forward);
        return true;
    }

    private async Task<bool> AllowAsync(NavigationEntry target, long version)
    {
        var context = new LocationChangingContext(target, Current);
        foreach (var registration in changing.ToArray())
        {
            if (registration.Owner is { } owner && !ReferenceEquals(owner, context.Current)) continue;
            await registration.Handler(context).ConfigureAwait(true);
            if (context.IsNavigationPrevented || version != requestVersion) return false;
        }
        return true;
    }

    private void Changed(NavigationKind kind) => LocationChanged?.Invoke(this, new LocationChangedEventArgs(Current, kind));

    private async void Observe(Task task)
    {
        try
        {
            await task.ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            if (UnhandledException is { } report) report(exception);
            else throw;
        }
    }

    private static string Normalize(string uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var query = uri.IndexOfAny(['?', '#']);
        var (path, rest) = query < 0 ? (uri, "") : (uri[..query], uri[query..]);
        path = "/" + path.Trim('/');
        return path + rest;
    }

    private sealed class Registration(Action remove) : IDisposable
    {
        private Action? remove = remove;

        public void Dispose()
        {
            remove?.Invoke();
            remove = null;
        }
    }

    private sealed class LocationGuard(Func<LocationChangingContext, ValueTask> handler, NavigationEntry? owner)
    {
        public Func<LocationChangingContext, ValueTask> Handler { get; } = handler;
        public NavigationEntry? Owner { get; } = owner;
    }
}
