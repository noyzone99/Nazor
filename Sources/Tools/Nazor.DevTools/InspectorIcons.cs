namespace Nazor.DevTools;

/// <summary>The inspector's monochrome interface icons, embedded from Assets/Icons and tinted by <c>Icon</c>.</summary>
internal static class InspectorIcons
{
    public static readonly ImageSource Pick = Load("pick");
    public static readonly ImageSource Components = Load("components");
    public static readonly ImageSource Elements = Load("elements");
    public static readonly ImageSource Performance = Load("performance");
    public static readonly ImageSource Refresh = Load("refresh");
    public static readonly ImageSource ExpandAll = Load("expand-all");
    public static readonly ImageSource CollapseAll = Load("collapse-all");
    public static readonly ImageSource Reveal = Load("reveal");
    public static readonly ImageSource Eye = Load("eye");
    public static readonly ImageSource EyeOff = Load("eye-off");
    public static readonly ImageSource Settings = Load("settings");
    public static readonly ImageSource Plug = Load("plug");
    public static readonly ImageSource Unplug = Load("unplug");
    public static readonly ImageSource Search = Load("search");
    public static readonly ImageSource Clear = Load("clear");
    public static readonly ImageSource ChevronDown = Load("chevron-down");
    public static readonly ImageSource Window = Load("window");
    public static readonly ImageSource Warning = Load("warning");
    public static readonly ImageSource Info = Load("info");
    public static readonly ImageSource Pause = Load("pause");
    public static readonly ImageSource Arrow = Load("arrow");
    public static readonly ImageSource Close = Load("close");
    public static readonly ImageSource Layers = Load("layers");

    private static ImageSource Load(string name) =>
        ImageSource.FromResource(typeof(InspectorIcons).Assembly, "Nazor.DevTools.Assets.Icons." + name + ".svg");
}
