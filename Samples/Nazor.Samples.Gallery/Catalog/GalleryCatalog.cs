namespace Nazor.Samples.Gallery;

// One catalog page: the component it documents, the example component that shows it and the example's own source.
public sealed record GalleryEntry(string Id, string Title, string Category, string Description, Type ComponentType, Type SampleType, params Type[] Parts)
{
    public string Code { get; private init; } = ReadSource(SampleType);
    public IEnumerable<Type> ComponentTypes => Parts.Prepend(ComponentType);

    public bool Matches(string query)
    {
        var term = query.Trim();
        return $"{Title} {Category} {Description}".Contains(term, StringComparison.OrdinalIgnoreCase) ||
            ComponentTypes.Any(type => type.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    internal GalleryEntry WithSupportingSource<T>() => this with
    {
        Code = $"@* {SampleType.Name}.nazor *@\n{Code}\n\n@* {typeof(T).Name}.nazor *@\n{ReadSource(typeof(T))}",
    };

    // Every example is embedded as its own resource, so the page shows exactly the code that runs.
    private static string ReadSource(Type sample)
    {
        using var stream = sample.Assembly.GetManifestResourceStream($"GallerySamples.{sample.Name}.nazor")
            ?? throw new InvalidOperationException($"Missing source for {sample.Name}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

// The catalog is split into one file per component group, so each group keeps its pages together.
public static partial class GalleryCatalog
{
    public static IReadOnlyList<string> Categories { get; } =
    [
        "Text and content", "Layout and surfaces", "Actions", "Selection", "Fields", "Pickers",
        "Collections", "Navigation", "Menus and overlays", "Feedback", "App frame", "Primitives",
    ];

    public static IReadOnlyList<GalleryEntry> Entries { get; } =
    [
        .. TextEntries(), .. LayoutEntries(), .. ActionEntries(), .. SelectionEntries(), .. FieldEntries(),
        .. PickerEntries(), .. CollectionEntries(), .. DataEntries(), .. NavigationEntries(), .. OverlayEntries(),
        .. FeedbackEntries(), .. FrameEntries(), .. PrimitiveEntries(),
    ];
}
