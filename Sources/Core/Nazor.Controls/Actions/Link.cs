using System.Diagnostics;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>
/// Opens addresses for <see cref="Link" />: the default program of the system, or what a test or a host sets. Only
/// absolute <c>http</c>, <c>https</c> and <c>mailto</c> addresses are opened.
/// </summary>
public static class Launcher
{
    /// <summary>
    /// Gets or sets what opens an address; by default the shell (the browser for http and https, the mail program for
    /// mailto). Set it from the UI thread, for example in tests or in a sandboxed host.
    /// </summary>
    public static Action<Uri> Open { get; set; } = uri => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();

    /// <summary>Gets whether an address is one the launcher opens: absolute, with the http, https or mailto scheme.</summary>
    /// <param name="uri">The address to check.</param>
    /// <returns>true if <see cref="TryOpen" /> would open the address; otherwise, false.</returns>
    public static bool CanOpen(Uri? uri) => uri is { IsAbsoluteUri: true } && uri.Scheme is "http" or "https" or "mailto";

    /// <summary>Opens an address with <see cref="Open" /> when <see cref="CanOpen" /> accepts it.</summary>
    /// <param name="uri">The address to open.</param>
    /// <returns>true if the address was handed to <see cref="Open" />; otherwise, false.</returns>
    public static bool TryOpen(Uri? uri)
    {
        if (!CanOpen(uri)) return false;
        Open(uri!);
        return true;
    }
}

/// <summary>
/// A hyperlink: activation by a click, Enter or Space runs <see cref="OnClick" /> and then opens
/// <see cref="NavigateUri" /> through the <see cref="Launcher" /> (absolute http, https and mailto addresses only). After
/// the first activation the element has the <c>visited</c> attribute until the link is removed. With
/// <see cref="IsExternal" /> a <c>LinkExternalMark</c> part follows the text and assistive technologies hear that the
/// link opens outside the application. The element has the <c>link</c> role; the address is its tooltip unless
/// <see cref="Control.Tooltip" /> is set.
/// </summary>
public sealed class Link : ContentControl
{
    private bool visited;

    /// <summary>Gets or sets the address opened when the link is activated.</summary>
    [Parameter] public Uri? NavigateUri { get; set; }

    /// <summary>Gets or sets the callback invoked when the link is activated, before the address is opened.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnClick { get; set; }

    /// <summary>Gets or sets whether the link opens outside the application, which adds an external mark.</summary>
    [Parameter] public bool IsExternal { get; set; }

    /// <summary>Gets or sets whether the link rejects interaction.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Gets whether the link has been activated since it was rendered.</summary>
    public bool Visited => visited;

    /// <summary>Gets the NSS element name emitted by Link.</summary>
    protected override string ElementName => "Link";

    /// <summary>Adds the link role, the states and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "link");
        builder.AddAttribute(11, "disabled", Disabled);
        builder.AddAttribute(12, "focusable", !Disabled);
        builder.AddAttribute(13, "visited", visited);
        builder.AddAttribute(14, "external", IsExternal);
        builder.AddAttribute(15, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ActivateAsync));
        if (Tooltip is null && NavigateUri is { IsAbsoluteUri: true } uri) builder.AddAttribute(16, "tooltip", uri.OriginalString);
        if (IsExternal && AccessibleDescription is null) builder.AddAttribute(17, "accessible-description", ControlStrings.Get("OpensExternally"));
    }

    /// <summary>Adds the text and the external mark.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        if (ChildContent is not null) builder.AddContent(1000, ChildContent);
        else builder.AddContent(1001, NavigateUri?.OriginalString);
        if (!IsExternal) return;
        builder.OpenElement(1010, "LinkExternalMark");
        builder.AddAttribute(1011, "semantic-hidden", true);
        builder.AddContent(1012, "↗");
        builder.CloseElement();
    }

    private async Task ActivateAsync(PointerEventArgs e)
    {
        if (Disabled) return;
        await OnClick.InvokeAsync(e);
        Launcher.TryOpen(NavigateUri);
        if (visited) return;
        visited = true;
        StateHasChanged();
    }
}
