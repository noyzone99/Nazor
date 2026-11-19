namespace Nazor;

public sealed partial class NazorWindow
{
    private WindowDecorationPolicy? decorationPolicy;
    private readonly SemaphoreSlim decorationGate = new(1, 1);
    private readonly CancellationTokenSource decorationLifetime = new();
    private bool openingDecorations, singleViewDecorations;

    /// <inheritdoc />
    public WindowDecorationPolicy Decorations
    {
        get => decorationPolicy ?? WindowDecorationPolicy.Default;
        set
        {
            AccessConfiguration();
            ArgumentNullException.ThrowIfNull(value);
            if (platformWindow is not null) throw new InvalidOperationException("SetDecorationsAsync changes an open window's decoration policy.");
            decorationPolicy = value;
        }
    }

    /// <inheritdoc />
    public WindowDecorationResult? AppliedDecorations { get; private set; }

    private WindowDecorations? InitialDecorations(bool singleView)
    {
        if (singleView)
        {
            singleViewDecorations = true;
            if (decorationPolicy is not null) throw new PlatformNotSupportedException("Single-view hosts do not provide desktop window decorations.");
            return null;
        }
        var selection = backendCapabilities.Decorations.Select(Decorations, false, out var reason);
        if (selection is null) throw new PlatformNotSupportedException(reason);
        ValidateDecorationContent(selection.Configuration);
        openingDecorations = true;
        return selection.Configuration;
    }

    private void InitializeDecorations()
    {
        if (!openingDecorations) return;
        _ = Dispatcher.InvokeAsync(() =>
        {
            var apply = ApplyDecorationsAsync(Decorations, true, default);
            if (apply.IsCompletedSuccessfully) _ = apply.Result;
            else _ = CompleteOpeningDecorationsAsync(apply);
        });
    }

    private async Task CompleteOpeningDecorationsAsync(ValueTask<WindowDecorationResult> apply)
    {
        try { await apply; }
        catch (OperationCanceledException) when (disposed) { }
        catch (Exception error)
        {
            if (!disposed) { OnException(error); Close(); }
        }
    }

    /// <inheritdoc />
    public ValueTask<WindowDecorationResult> SetDecorationsAsync(WindowDecorationPolicy policy, CancellationToken cancellationToken = default)
    {
        AccessConfiguration();
        ArgumentNullException.ThrowIfNull(policy);
        if (singleViewDecorations) throw new PlatformNotSupportedException("Single-view hosts do not provide desktop window decorations.");
        if (platformWindow is null) throw new InvalidOperationException("Assign Decorations while initializing the window.");
        return new(Dispatcher.InvokeAsync(() => ApplyDecorationsAsync(policy, false, cancellationToken).AsTask())
            .WaitAsync(decorationLifetime.Token));
    }

    private async ValueTask<WindowDecorationResult> ApplyDecorationsAsync(WindowDecorationPolicy policy, bool opening, CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, decorationLifetime.Token);
        await decorationGate.WaitAsync(cancellation.Token);
        var previous = AppliedDecorations;
        var dispatched = false;
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellation.Token.ThrowIfCancellationRequested();
            var reasons = new List<string>();
            for (var index = 0; index < policy.Options.Count; index++)
            {
                var configuration = policy.Options[index];
                var capabilities = PlatformWindow.Capabilities.Decorations with { Current = previous?.Configuration };
                var support = opening ? capabilities.GetSupport(configuration) : capabilities.GetChangeSupport(configuration);
                if (support.Status == WindowSupportStatus.Unsupported) { reasons.Add($"Option {index}: {support.Reason}"); continue; }
                ValidateDecorationContent(configuration);
                try
                {
                    dispatched = true;
                    var accepted = await PlatformWindow.SetDecorationsAsync(configuration, decorationLifetime.Token);
                    ObjectDisposedException.ThrowIf(disposed, this);
                    if (accepted != configuration) throw new InvalidOperationException("The backend substituted an unauthorized decoration configuration.");
                    AppliedDecorations = new(accepted, index);
                    decorationPolicy = policy;
                    openingDecorations = false;
                    Scene.Viewport = PlatformWindow.ClientSize;
                    RefreshCapabilities();
                    RefreshSystemButtons();
                    NotifyPresentation();
                    RequestFrame(WindowFrameCause.RenderBatch);
                    return AppliedDecorations;
                }
                catch (PlatformNotSupportedException error) { reasons.Add($"Option {index}: {error.Message}"); }
            }
            throw new PlatformNotSupportedException(string.Join(" ", reasons));
        }
        catch (Exception error) when (dispatched && previous is not null && !disposed)
        {
            try { await PlatformWindow.SetDecorationsAsync(previous.Configuration, decorationLifetime.Token); }
            catch (Exception restoreError)
            {
                if (!disposed) { RefreshCapabilities(); RefreshSystemButtons(); }
                throw new AggregateException("The decoration change failed and the previous configuration could not be restored.", error, restoreError);
            }
            RefreshCapabilities();
            RefreshSystemButtons();
            throw;
        }
        finally { decorationGate.Release(); }
    }

    private void ValidateDecorationContent(WindowDecorations configuration)
    {
        var owner = DecorationOwner;
        if (configuration.TitleBar == WindowTitleBarContent.User && owner?.TitleBarCount != 1)
            throw new InvalidOperationException("The selected User title bar requires one UserTitleBar declaration in the window.");
        if (configuration.CaptionButtons == WindowCaptionButtons.User && owner?.CaptionButtonsCount != 1)
            throw new InvalidOperationException("The selected User caption buttons require one nonempty UserCaptionButtons declaration in the window.");
    }
}
