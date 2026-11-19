using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls.Primitives;

/// <summary>What a <see cref="LoadingBoundary" /> currently shows, written as its <c>state</c> attribute.</summary>
public enum LoadingBoundaryState
{
    /// <summary>The content (<c>content</c>).</summary>
    Content,
    /// <summary>Nothing, because loading started less than the delay ago (<c>pending</c>).</summary>
    Pending,
    /// <summary>The loading indicator (<c>loading</c>).</summary>
    Loading,
    /// <summary>The error with its retry button (<c>error</c>).</summary>
    Error,
}

/// <summary>
/// Shows content that loads asynchronously without flicker: while <see cref="IsLoading" /> is true it shows nothing for
/// <see cref="Delay" />, then the <see cref="Loading" /> content (a <see cref="Spinner" /> by default) for at least
/// <see cref="MinimumDuration" />, so fast loads show no indicator and slow ones do not flash it. When loading is over
/// and <see cref="Error" /> or <see cref="ErrorMessage" /> is set it shows the error (<see cref="ErrorContent" />, or the
/// message and a retry button raising <see cref="OnRetry" />); otherwise the content between the tags. The parts are
/// <c>LoadingBoundaryLoading</c>, <c>LoadingBoundaryError</c> (a live region) with <c>LoadingBoundaryMessage</c> and
/// <c>LoadingBoundaryRetry</c>, and <c>LoadingBoundaryContent</c>.
/// </summary>
public sealed class LoadingBoundary : ContentControl, IDisposable
{
    private bool loadingRequested;
    private bool indicatorShown;
    private DateTimeOffset shownAt;
    private ITimer? timer;
    private int generation;
    private bool disposed;

    /// <summary>Gets or sets whether the content is loading.</summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>Gets or sets the failure of the last load, or null.</summary>
    [Parameter] public Exception? Error { get; set; }

    /// <summary>Gets or sets the text shown for the failure; null uses the exception's message.</summary>
    [Parameter] public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the callback invoked by the retry button; without it no retry button is shown.</summary>
    [Parameter] public EventCallback OnRetry { get; set; }

    /// <summary>Gets or sets the content shown while loading; null shows a <see cref="Spinner" />.</summary>
    [Parameter] public RenderFragment? Loading { get; set; }

    /// <summary>Gets or sets the content shown for a failure, which receives the message; null shows the default error.</summary>
    [Parameter] public RenderFragment<string>? ErrorContent { get; set; }

    /// <summary>Gets or sets how long loading must last before the indicator appears; 300 ms by default.</summary>
    [Parameter] public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Gets or sets how long the indicator stays once shown, even if loading ends sooner; 500 ms by default.</summary>
    [Parameter] public TimeSpan MinimumDuration { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Gets or sets the clock used for the delays, or null for the system clock.</summary>
    [Parameter] public TimeProvider? Time { get; set; }

    /// <summary>Gets what the boundary currently shows.</summary>
    public LoadingBoundaryState State =>
        indicatorShown ? LoadingBoundaryState.Loading
        : loadingRequested ? LoadingBoundaryState.Pending
        : HasError ? LoadingBoundaryState.Error
        : LoadingBoundaryState.Content;

    private bool HasError => Error is not null || ErrorMessage is not null;

    private TimeProvider Clock => Time ?? TimeProvider.System;

    /// <summary>Gets the NSS element name emitted by LoadingBoundary.</summary>
    protected override string ElementName => "LoadingBoundary";

    /// <summary>Validates the delays and follows the loading state.</summary>
    protected override void OnParametersSet()
    {
        if (Delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(Delay));
        if (MinimumDuration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(MinimumDuration));
        if (IsLoading == loadingRequested) return;
        loadingRequested = IsLoading;
        if (IsLoading)
        {
            if (indicatorShown)
            {
                Cancel();
                return;
            }
            if (Delay == TimeSpan.Zero) ShowIndicator();
            else Start(Delay, ShowIndicator);
            return;
        }
        if (!indicatorShown)
        {
            Cancel();
            return;
        }
        var left = shownAt + MinimumDuration - Clock.GetUtcNow();
        if (left <= TimeSpan.Zero) HideIndicator();
        else Start(left, HideIndicator);
    }

    private void ShowIndicator()
    {
        Cancel();
        indicatorShown = true;
        shownAt = Clock.GetUtcNow();
    }

    private void HideIndicator()
    {
        Cancel();
        indicatorShown = false;
    }

    private void Start(TimeSpan due, Action work)
    {
        Cancel();
        var current = generation;
        timer = Clock.CreateTimer(_ => _ = InvokeAsync(() =>
        {
            if (disposed || current != generation) return;
            work();
            StateHasChanged();
        }), null, due, Timeout.InfiniteTimeSpan);
    }

    private void Cancel()
    {
        generation++;
        timer?.Dispose();
        timer = null;
    }

    /// <summary>Adds the state and the busy attribute.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "state", State.ToString().ToLowerInvariant());
        builder.AddAttribute(11, "busy", loadingRequested || indicatorShown);
    }

    /// <summary>Adds the part of the current state.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        switch (State)
        {
            case LoadingBoundaryState.Loading:
                builder.OpenElement(1000, "LoadingBoundaryLoading");
                if (Loading is not null) builder.AddContent(1001, Loading);
                else
                {
                    builder.OpenComponent<Spinner>(1002);
                    builder.AddComponentParameter(1003, nameof(Spinner.Label), ControlStrings.Get("Loading"));
                    builder.CloseComponent();
                }
                builder.CloseElement();
                break;
            case LoadingBoundaryState.Error:
                var message = ErrorMessage ?? Error?.Message ?? ControlStrings.Get("Error");
                builder.OpenElement(1010, "LoadingBoundaryError");
                builder.AddAttribute(1011, "role", "status");
                builder.AddAttribute(1012, "live", "assertive");
                if (ErrorContent is not null) builder.AddContent(1013, ErrorContent(message));
                else
                {
                    builder.OpenElement(1014, "LoadingBoundaryMessage");
                    builder.AddAttribute(1015, "role", "text");
                    builder.AddContent(1016, message);
                    builder.CloseElement();
                    if (OnRetry.HasDelegate)
                    {
                        builder.OpenElement(1017, "LoadingBoundaryRetry");
                        builder.AddAttribute(1018, "role", "button");
                        builder.AddAttribute(1019, "focusable", true);
                        builder.AddAttribute(1020, "onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => OnRetry.InvokeAsync()));
                        builder.AddContent(1021, ControlStrings.Get("Retry"));
                        builder.CloseElement();
                    }
                }
                builder.CloseElement();
                break;
            case LoadingBoundaryState.Content:
                builder.OpenElement(1030, "LoadingBoundaryContent");
                builder.AddContent(1031, ChildContent);
                builder.CloseElement();
                break;
        }
    }

    /// <summary>Stops the pending delay.</summary>
    public void Dispose()
    {
        disposed = true;
        Cancel();
    }
}
