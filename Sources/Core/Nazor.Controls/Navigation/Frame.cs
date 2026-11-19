using System.Globalization;
using System.Reflection;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;
using Nazor.Routing;

namespace Nazor.Controls;

/// <summary>
/// Shows the page of the current place of the window's <see cref="NavigationManager" />: the page whose <c>@page</c>
/// route matches the address, with the route values as its parameters, or the page reached by type. A new place
/// creates a new page. Pages are looked for in <see cref="Assemblies" /> (the application by default); without one,
/// <see cref="NotFound" /> shows. Alt+Left and Alt+Right, the Back and Forward keys and the side buttons of the mouse
/// go back and forward. <see cref="Transition"/> requests theme-owned page motion through <c>FrameTransition</c> and
/// <c>FrameTransitionLayer</c>. An outgoing page is disabled and hidden from accessibility, then disposed after the
/// retained exit. Page-owned navigation guards should use the overload that accepts their exact navigation entry.
/// </summary>
public sealed class Frame : Control, IDisposable
{
    private RouteTable? table;
    private Assembly[] tableAssemblies = [];
    private IDisposable[] keys = [];
    private int generation;
    private TransitionDirection direction;

    /// <summary>Gets or sets the injected navigation manager for this window.</summary>
    [Inject] public NavigationManager Navigation { get; set; } = null!;

    /// <summary>Gets or sets the injected keyboard command registry.</summary>
    [Inject] public KeyBindings Keys { get; set; } = null!;

    /// <summary>Where the pages are; the entry assembly of the application by default.</summary>
    [Parameter] public IEnumerable<Assembly>? Assemblies { get; set; }

    /// <summary>What shows for an address without a page.</summary>
    [Parameter] public RenderFragment? NotFound { get; set; }

    /// <summary>Gets or sets the motion requested for page replacements; themes control its appearance and retention.</summary>
    [Parameter] public ContentTransitionKind Transition { get; set; } = ContentTransitionKind.Entrance;

    /// <summary>Gets the NSS element name emitted by Frame.</summary>
    protected override string ElementName => "Frame";

    /// <summary>Initializes component state before the first parameter update completes.</summary>
    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
        keys =
        [
            Keys.Register("ArrowLeft", KeyModifiers.Alt, EventCallback.Factory.Create<KeyboardEventArgs>(this, Navigation.GoBack)),
            Keys.Register("ArrowRight", KeyModifiers.Alt, EventCallback.Factory.Create<KeyboardEventArgs>(this, Navigation.GoForward)),
            Keys.Register("BrowserBack", KeyModifiers.None, EventCallback.Factory.Create<KeyboardEventArgs>(this, Navigation.GoBack)),
            Keys.Register("BrowserForward", KeyModifiers.None, EventCallback.Factory.Create<KeyboardEventArgs>(this, Navigation.GoForward)),
        ];
    }

    /// <summary>Updates component state after parameter values have been assigned.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Transition)) throw new ArgumentOutOfRangeException(nameof(Transition));
        Assembly[] assemblies = Assemblies is { } given ? [.. given] : Assembly.GetEntryAssembly() is { } entry ? [entry] : [];
        if (table is not null && assemblies.SequenceEqual(tableAssemblies)) return;
        tableAssemblies = assemblies;
        table = RouteTable.Create(assemblies);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        direction = e.Kind == NavigationKind.Back ? TransitionDirection.Backward : TransitionDirection.Forward;
        generation++;
        StateHasChanged();
    }

    /// <summary>Adds the child render frames used by Frame.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var entry = Navigation.Current;
        var (page, values) = entry.Page is { } type ? (type, entry.Parameters) :
            entry.Uri is { } uri && table?.Match(uri) is { } match ? (match.Page, match.Parameters) : (null, null);
        var parameters = new Dictionary<string, object?>();
        foreach (var (name, value) in values ?? new Dictionary<string, object?>())
            if (page is not null && Parameter(page, name) is { } property) parameters[property.Name] = ConvertTo(value, property.PropertyType);
        var notFound = NotFound;
        builder.OpenComponent<ContentTransition>(1000);
        builder.AddComponentParameter(1001, nameof(ContentTransition.As), "FrameTransition");
        builder.AddComponentParameter(1002, nameof(ContentTransition.ContentKey), generation);
        builder.AddComponentParameter(1003, nameof(ContentTransition.Transition), direction == TransitionDirection.Backward ? Transition switch
        {
            ContentTransitionKind.DrillIn => ContentTransitionKind.DrillOut,
            ContentTransitionKind.DrillOut => ContentTransitionKind.DrillIn,
            _ => Transition,
        } : Transition);
        builder.AddComponentParameter(1004, nameof(ContentTransition.Direction), direction);
        builder.AddComponentParameter(1005, nameof(ContentTransition.ChildContent), (RenderFragment)(content =>
        {
            if (page is null) { content.AddContent(0, notFound); return; }
            content.OpenComponent(1, page);
            foreach (var (name, value) in parameters) content.AddComponentParameter(2, name, value);
            content.CloseComponent();
        }));
        builder.CloseComponent();
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Nazor.Sdk preserves every page and its parameters in components.xml.")]
    private static PropertyInfo? Parameter(Type page, string name) =>
        page.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is { } property &&
        property.GetCustomAttribute<ParameterAttribute>() is not null ? property : null;

    private static object? ConvertTo(object? value, Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (value is null || target.IsInstanceOfType(value) || value is not string text) return value;
        try
        {
            return target.IsEnum ? Enum.Parse(target, text, ignoreCase: true) : System.Convert.ChangeType(text, target, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return value;
        }
    }

    /// <summary>Releases the resources and event subscriptions owned by this instance.</summary>
    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        foreach (var key in keys) key.Dispose();
        keys = [];
    }
}
