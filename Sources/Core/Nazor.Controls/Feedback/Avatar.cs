using System.Globalization;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The availability of the person an <see cref="Avatar" /> represents, written as the <c>presence</c> attribute.</summary>
public enum AvatarPresence
{
    /// <summary>No availability is shown.</summary>
    None,
    /// <summary>Available (<c>online</c>).</summary>
    Online,
    /// <summary>Away from the device (<c>away</c>).</summary>
    Away,
    /// <summary>Busy or not to be disturbed (<c>busy</c>).</summary>
    Busy,
    /// <summary>Not connected (<c>offline</c>).</summary>
    Offline,
}

/// <summary>
/// The picture of a person or an entity. It shows the <see cref="Source" /> image (<c>AvatarImage</c>); while the image
/// loads, when it fails or without one it shows the initials (<c>AvatarInitials</c>) of <see cref="Name" /> or
/// <see cref="Initials" />, or else the generic <c>AvatarFallback</c> part. <see cref="Presence" /> adds the
/// <c>AvatarPresence</c> part and <see cref="Badge" /> the <c>AvatarBadge</c> part at a corner. The element is an image
/// named by <see cref="Name" /> and the presence ("Ada Lovelace, online"); it has the <c>size</c>, <c>shape</c> and
/// <c>presence</c> attributes, and <see cref="PixelSize" /> sets an explicit size through <c>--nz-avatar-size</c>.
/// Inside an <see cref="AvatarGroup" /> it takes the group's size and may be hidden by the group's limit.
/// </summary>
public sealed class Avatar : Control, IDisposable
{
    private ImageSource? failed;
    private ImageSource? loaded;

    /// <summary>Gets or sets the picture, or null to show initials.</summary>
    [Parameter] public ImageSource? Source { get; set; }

    /// <summary>Gets or sets the name of the person, used for the accessible name and the initials.</summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>Gets or sets the initials shown instead of those computed from <see cref="Name" />.</summary>
    [Parameter] public string? Initials { get; set; }

    /// <summary>Gets or sets the availability shown at a corner.</summary>
    [Parameter] public AvatarPresence Presence { get; set; }

    /// <summary>Gets or sets content shown at the top corner, usually a standalone <see cref="Controls.Badge" />.</summary>
    [Parameter] public RenderFragment? Badge { get; set; }

    /// <summary>Gets or sets the size step; inside an <see cref="AvatarGroup" /> the group's size is used.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets or sets an explicit width and height in logical pixels, or null to use <see cref="Size" />.</summary>
    [Parameter] public double? PixelSize { get; set; }

    /// <summary>Gets or sets the outline; a circle by default, or <see cref="Controls.Shape.Square" />.</summary>
    [Parameter] public Shape Shape { get; set; } = Shape.Circle;

    /// <summary>Gets or sets the enclosing group, supplied automatically.</summary>
    [CascadingParameter] public AvatarGroup? Group { get; set; }

    /// <summary>Gets the initials shown when there is no picture, or null when there are none.</summary>
    public string? DisplayInitials => Initials is { Length: > 0 } initials ? initials : InitialsOf(Name);

    /// <summary>Gets the NSS element name emitted by Avatar.</summary>
    protected override string ElementName => "Avatar";

    /// <summary>Gets the explicit size variable and the caller's declarations.</summary>
    protected override string? ElementStyle => PixelSize is { } size ? Css.Join("--nz-avatar-size: " + Css.Px(size), Style) : Style;

    /// <summary>Joins the enclosing group.</summary>
    protected override void OnInitialized() => Group?.Join(this);

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Presence)) throw new ArgumentOutOfRangeException(nameof(Presence));
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Shape)) throw new ArgumentOutOfRangeException(nameof(Shape));
        if (PixelSize is { } size && (!double.IsFinite(size) || size <= 0)) throw new ArgumentOutOfRangeException(nameof(PixelSize), "The size must be positive.");
    }

    /// <summary>Renders nothing when the enclosing group hides this avatar.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Group?.IsShown(this) != false) base.BuildRenderTree(builder);
    }

    /// <summary>Adds the image role, the name and the look attributes.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "image");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", NameWithPresence());
        builder.AddAttribute(12, "size", Vocabulary.Name(Group?.Size ?? Size));
        builder.AddAttribute(13, "shape", Vocabulary.Name(Shape));
        builder.AddAttribute(14, "presence", Presence == AvatarPresence.None ? null : PresenceName(Presence));
        if (Group is { } group)
        {
            var (index, count) = group.Place(this);
            builder.AddAttribute(15, "position", Vocabulary.Name(Vocabulary.Position(index, count)));
        }
    }

    /// <summary>Adds the image, the initials or fallback, the presence and the badge.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var usable = Source is not null && !Equals(Source, failed);
        if (!usable || !Equals(Source, loaded))
        {
            if (DisplayInitials is { } initials)
            {
                builder.OpenElement(1000, "AvatarInitials");
                builder.AddContent(1001, initials);
                builder.CloseElement();
            }
            else
            {
                builder.OpenElement(1005, "AvatarFallback");
                builder.CloseElement();
            }
        }
        if (usable)
        {
            builder.OpenElement(1010, "AvatarImage");
            builder.AddAttribute(1011, "source", Source);
            builder.AddAttribute(1012, "onload", EventCallback.Factory.Create<ImageLoadEventArgs>(this, Loaded));
            builder.AddAttribute(1013, "onerror", EventCallback.Factory.Create<ImageErrorEventArgs>(this, Failed));
            builder.CloseElement();
        }
        if (Presence != AvatarPresence.None)
        {
            builder.OpenElement(1020, "AvatarPresence");
            builder.AddAttribute(1021, "presence", PresenceName(Presence));
            builder.AddAttribute(1022, "semantic-hidden", true);
            builder.CloseElement();
        }
        if (Badge is not null)
        {
            builder.OpenElement(1030, "AvatarBadge");
            builder.AddContent(1031, Badge);
            builder.CloseElement();
        }
    }

    private void Loaded(ImageLoadEventArgs e) => loaded = Source;

    private void Failed(ImageErrorEventArgs e) => failed = Source;

    internal void Refresh() => _ = InvokeAsync(StateHasChanged);

    private string? NameWithPresence() =>
        Presence == AvatarPresence.None ? Name : string.Join(", ", new[] { Name, ControlStrings.Get(Presence.ToString()) }.Where(s => !string.IsNullOrEmpty(s)));

    private static string PresenceName(AvatarPresence presence) => presence.ToString().ToLowerInvariant();

    /// <summary>Computes up to two initials from a name: the first letters of its first and last words.</summary>
    /// <param name="name">The name, such as "Ada Lovelace".</param>
    /// <returns>The initials in upper case, such as "AL", or null when the name has no letters or digits.</returns>
    public static string? InitialsOf(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Where(word => char.IsLetterOrDigit(word, 0)).ToArray();
        if (words.Length == 0) return null;
        var initials = StringInfo.GetNextTextElement(words[0]);
        if (words.Length > 1) initials += StringInfo.GetNextTextElement(words[^1]);
        return initials.ToUpper(CultureInfo.CurrentCulture);
    }

    /// <summary>Leaves the enclosing group.</summary>
    public void Dispose() => Group?.Leave(this);
}

/// <summary>
/// A row of overlapping <see cref="Avatar" /> controls, such as the members of a conversation. Only the first
/// <see cref="Max" /> avatars are shown; the <c>AvatarGroupOverflow</c> part counts the others ("+3"). Each avatar gets
/// the group's size and a <c>position</c> attribute (<c>first</c>, <c>middle</c>, <c>last</c>) so themes can overlap
/// them. The element is a group named by the number of people, unless <see cref="Control.AccessibleName" /> is set.
/// </summary>
public sealed class AvatarGroup : ContentControl
{
    private readonly List<Avatar> members = [];

    /// <summary>Gets or sets the largest number of avatars shown, or null to show them all.</summary>
    [Parameter] public int? Max { get; set; }

    /// <summary>Gets or sets the size step of every avatar in the group.</summary>
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>Gets the number of avatars in the group.</summary>
    public int Count => members.Count;

    /// <summary>Gets the number of avatars hidden by <see cref="Max" />.</summary>
    public int HiddenCount => Max is { } max ? Math.Max(0, members.Count - max) : 0;

    /// <summary>Gets the NSS element name emitted by AvatarGroup.</summary>
    protected override string ElementName => "AvatarGroup";

    /// <summary>Validates the parameters and updates the avatars after a change of limit or size.</summary>
    protected override void OnParametersSet()
    {
        if (Max < 1) throw new ArgumentOutOfRangeException(nameof(Max), "At least one avatar must be shown.");
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        foreach (var member in members) member.Refresh();
    }

    /// <summary>Adds the group role, its name and the size.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null) builder.AddAttribute(11, "accessible-name", ControlStrings.Format("PeopleCount", members.Count));
        builder.AddAttribute(12, "size", Vocabulary.Name(Size));
    }

    /// <summary>Adds the avatars and the overflow count.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<AvatarGroup>>(1000);
        builder.AddComponentParameter(1001, "Value", this);
        builder.AddComponentParameter(1002, "IsFixed", true);
        builder.AddComponentParameter(1003, "ChildContent", ChildContent);
        builder.CloseComponent();
        var hidden = HiddenCount;
        if (hidden == 0) return;
        builder.OpenElement(1010, "AvatarGroupOverflow");
        builder.AddAttribute(1011, "role", "text");
        builder.AddAttribute(1012, "accessible-name", ControlStrings.Format("MoreCount", hidden));
        builder.AddAttribute(1013, "size", Vocabulary.Name(Size));
        builder.AddAttribute(1014, "position", "last");
        builder.AddContent(1015, ControlStrings.Format("AndMore", hidden));
        builder.CloseElement();
    }

    internal void Join(Avatar avatar)
    {
        members.Add(avatar);
        Changed();
    }

    internal void Leave(Avatar avatar)
    {
        if (members.Remove(avatar)) Changed();
    }

    internal bool IsShown(Avatar avatar) => Max is not { } max || members.IndexOf(avatar) is var index && index >= 0 && index < max;

    internal (int Index, int Count) Place(Avatar avatar)
    {
        var shown = Max is { } max ? Math.Min(max, members.Count) : members.Count;
        return (Math.Max(0, members.IndexOf(avatar)), shown + (HiddenCount > 0 ? 1 : 0));
    }

    private void Changed()
    {
        _ = InvokeAsync(StateHasChanged);
        foreach (var member in members) member.Refresh();
    }
}
