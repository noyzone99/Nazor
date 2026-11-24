using Microsoft.JSInterop;

namespace Nazor.Website.Services;

/// <summary>Wraps the small browser helpers in <c>wwwroot/js/site.js</c>.</summary>
public sealed class SiteInterop(IJSRuntime js)
{
    public ValueTask<SitePreferences> GetPreferencesAsync() => js.InvokeAsync<SitePreferences>("nazorSite.getPreferences");

    public ValueTask SetThemeAsync(string theme) => js.InvokeVoidAsync("nazorSite.setTheme", theme);

    public ValueTask SetAccentAsync(string accent) => js.InvokeVoidAsync("nazorSite.setAccent", accent);

    /// <summary>Enhances a freshly rendered document: scrolls to the hash, tracks the table of contents, renders diagrams.</summary>
    public ValueTask PageRenderedAsync(bool resetScroll) => js.InvokeVoidAsync("nazorSite.pageRendered", resetScroll);

    public ValueTask RegisterShortcutsAsync<T>(DotNetObjectReference<T> target) where T : class =>
        js.InvokeVoidAsync("nazorSite.registerShortcuts", target);

    public ValueTask FocusAsync(string selector) => js.InvokeVoidAsync("nazorSite.focus", selector);

    public ValueTask ScrollIntoViewAsync(string selector) => js.InvokeVoidAsync("nazorSite.scrollIntoView", selector);

    public ValueTask SetBodyLockedAsync(bool locked) => js.InvokeVoidAsync("nazorSite.setBodyLocked", locked);
}

public sealed class SitePreferences
{
    public string Theme { get; set; } = "system";

    public string Accent { get; set; } = "blue";
}
