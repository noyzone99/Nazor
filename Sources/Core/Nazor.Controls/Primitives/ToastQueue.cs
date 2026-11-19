namespace Nazor.Controls.Primitives;

/// <summary>A notification held by a <see cref="ToastQueue" />.</summary>
public sealed class ToastEntry
{
    private TimeSpan? remaining;
    private DateTimeOffset? deadline;

    internal ToastEntry(long id, ToastOptions options)
    {
        Id = id;
        Options = options;
        remaining = options.Duration;
    }

    /// <summary>Gets the identifier, unique within its queue.</summary>
    public long Id { get; }

    /// <summary>Gets the content and behavior requested for the notification.</summary>
    public ToastOptions Options { get; }

    /// <summary>Gets whether the notification is on screen rather than waiting for room.</summary>
    public bool IsVisible { get; internal set; }

    /// <summary>Gets whether the countdown is suspended, because the pointer or the focus is on it or the window is inactive.</summary>
    public bool IsPaused => pauses > 0;

    /// <summary>Gets the time left before the notification closes by itself, or null when it stays until dismissed.</summary>
    public TimeSpan? Remaining(DateTimeOffset now) =>
        remaining is null ? null : deadline is { } end ? end > now ? end - now : TimeSpan.Zero : remaining;

    internal int pauses;

    internal DateTimeOffset? Deadline => deadline;
    internal TimeSpan? CountdownDuration => deadline.HasValue ? remaining : null;

    internal void Start(DateTimeOffset now)
    {
        if (remaining is { } left && pauses == 0 && deadline is null) deadline = now + left;
    }

    internal void Pause(DateTimeOffset now)
    {
        if (pauses++ > 0 || deadline is not { } end) return;
        remaining = end > now ? end - now : TimeSpan.Zero;
        deadline = null;
    }

    internal void Resume(DateTimeOffset now)
    {
        if (pauses == 0 || --pauses > 0) return;
        if (IsVisible) Start(now);
    }
}

/// <summary>What a notification shows and how long it stays.</summary>
public sealed record ToastOptions
{
    /// <summary>Gets the main text, also announced to assistive technologies.</summary>
    public required string Message { get; init; }

    /// <summary>Gets an optional bold title shown before the message.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the severity, which selects the mark and the announcement urgency.</summary>
    public Severity Severity { get; init; } = Severity.Info;

    /// <summary>Gets how long the notification stays; null keeps it until it is dismissed. Defaults to five seconds.</summary>
    public TimeSpan? Duration { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets the text of an optional action button, such as Undo.</summary>
    public string? ActionText { get; init; }

    /// <summary>Gets the work run when the action button is pressed; the notification closes afterwards.</summary>
    public Func<Task>? Action { get; init; }

    /// <summary>Gets whether a close button is shown.</summary>
    public bool Dismissible { get; init; } = true;
}

/// <summary>
/// The queue behind toast notifications, independent of how they are drawn: a limit on the visible ones, the order of
/// arrival, countdowns that pause while the pointer or the focus is on a notification or the window is inactive, and
/// expiry. A region component draws <see cref="Visible" /> and calls <see cref="Expire" /> when <see cref="NextDeadline" />
/// passes. Every member must be used from the UI thread.
/// </summary>
/// <param name="time">The clock used for countdowns, or null for the system clock.</param>
public sealed class ToastQueue(TimeProvider? time = null)
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;
    private readonly List<ToastEntry> entries = [];
    private long next;
    private int maximum = 3;

    /// <summary>Gets or sets how many notifications are on screen at once; the others wait in order.</summary>
    public int MaxVisible
    {
        get => maximum;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            maximum = value;
            Promote();
        }
    }

    /// <summary>Gets the notifications on screen, oldest first.</summary>
    public IReadOnlyList<ToastEntry> Visible => [.. entries.Where(entry => entry.IsVisible)];

    /// <summary>Gets the number of notifications waiting for room.</summary>
    public int PendingCount => entries.Count(entry => !entry.IsVisible);

    /// <summary>Gets the moment at which the next visible notification expires, or null when none is counting down.</summary>
    public DateTimeOffset? NextDeadline => entries.Where(entry => entry.IsVisible).Select(entry => entry.Deadline).Where(d => d is not null).Min();

    /// <summary>Raised after notifications are added, shown, paused, resumed or removed.</summary>
    public event Action? Changed;

    /// <summary>Adds a notification; it appears at once when there is room.</summary>
    /// <param name="options">The content and behavior of the notification.</param>
    /// <returns>The queued notification.</returns>
    public ToastEntry Show(ToastOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Duration is { } duration && duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), "A toast duration must be positive.");
        var entry = new ToastEntry(++next, options);
        entries.Add(entry);
        Promote();
        Changed?.Invoke();
        return entry;
    }

    /// <summary>Adds a plain notification with a message.</summary>
    /// <param name="message">The text to show.</param>
    /// <param name="severity">The severity of the message.</param>
    /// <returns>The queued notification.</returns>
    public ToastEntry Show(string message, Severity severity = Severity.Info) => Show(new ToastOptions { Message = message, Severity = severity });

    /// <summary>Removes a notification, visible or waiting.</summary>
    /// <param name="entry">The notification to remove.</param>
    /// <returns>true if the notification was in the queue.</returns>
    public bool Dismiss(ToastEntry entry)
    {
        if (!entries.Remove(entry)) return false;
        Promote();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Removes every notification.</summary>
    public void Clear()
    {
        if (entries.Count == 0) return;
        entries.Clear();
        Changed?.Invoke();
    }

    /// <summary>Suspends the countdown of a notification; calls nest and must be balanced by <see cref="Resume" />.</summary>
    /// <param name="entry">The notification under the pointer or the focus.</param>
    public void Pause(ToastEntry entry)
    {
        entry.Pause(clock.GetUtcNow());
        Changed?.Invoke();
    }

    /// <summary>Resumes a countdown suspended by <see cref="Pause" />.</summary>
    /// <param name="entry">The notification that the pointer or the focus left.</param>
    public void Resume(ToastEntry entry)
    {
        entry.Resume(clock.GetUtcNow());
        Changed?.Invoke();
    }

    /// <summary>Suspends every countdown, for example while the window is inactive.</summary>
    public void PauseAll()
    {
        foreach (var entry in entries) entry.Pause(clock.GetUtcNow());
        Changed?.Invoke();
    }

    /// <summary>Resumes the countdowns suspended by <see cref="PauseAll" />.</summary>
    public void ResumeAll()
    {
        foreach (var entry in entries) entry.Resume(clock.GetUtcNow());
        Changed?.Invoke();
    }

    /// <summary>Removes the visible notifications whose time is over.</summary>
    /// <returns>true if any notification expired.</returns>
    public bool Expire()
    {
        var now = clock.GetUtcNow();
        var removed = entries.RemoveAll(entry => entry.IsVisible && entry.Deadline is { } end && end <= now);
        if (removed == 0) return false;
        Promote();
        Changed?.Invoke();
        return true;
    }

    private void Promote()
    {
        var now = clock.GetUtcNow();
        var shown = entries.Count(entry => entry.IsVisible);
        foreach (var entry in entries)
        {
            if (shown >= maximum) break;
            if (entry.IsVisible) continue;
            entry.IsVisible = true;
            entry.Start(now);
            shown++;
        }
    }
}
