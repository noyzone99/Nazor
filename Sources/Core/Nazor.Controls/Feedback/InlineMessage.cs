using Nazor.Controls.Primitives;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>How an <see cref="InlineMessage" /> arranges its parts, written as the <c>layout</c> attribute.</summary>
public enum InlineMessageLayout
{
    /// <summary>One line: the mark, the title, the message and the actions side by side (<c>compact</c>).</summary>
    Compact,
    /// <summary>The title and the message stacked, with the actions below them (<c>expanded</c>).</summary>
    Expanded,
}

/// <summary>
/// A message about the surrounding content, such as a form-wide error or a note above a list, in the flow of the page.
/// The parts are <c>InlineMessageMark</c> (a mark for the <see cref="Severity" />, named for assistive technologies),
/// <c>InlineMessageBody</c> with <c>InlineMessageTitle</c> and <c>InlineMessageText</c> (the content between the tags),
/// <c>InlineMessageActions</c> and, with <see cref="IsClosable" />, the <c>InlineMessageClose</c> button.
/// <see cref="IsOpen" /> supports <c>@bind-IsOpen</c>: closing hides the message. The message is a live region, so
/// showing it or changing its text is announced: errors at once (<c>assertive</c>), the others politely.
/// <c>InlineMessagePresence</c> and its measured content part retain and collapse the complete message during a
/// theme-controlled exit. The message's element reference, classes and declarations remain on <c>InlineMessage</c>.
/// </summary>
public sealed class InlineMessage : ContentControl
{
    private readonly ControllableState<bool> open = new(true);
    private bool exiting;

    /// <summary>Gets or sets the importance of the message.</summary>
    [Parameter] public Severity Severity { get; set; }

    /// <summary>Gets or sets the bold title shown before the message.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Gets or sets actions related to the message, usually buttons.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Gets or sets whether a close button is shown.</summary>
    [Parameter] public bool IsClosable { get; set; }

    /// <summary>Gets or sets whether the message is shown; true by default.</summary>
    [Parameter] public bool IsOpen { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the close button hides the message.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the callback invoked after the close button hides the message.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Gets or sets how the parts are arranged.</summary>
    [Parameter] public InlineMessageLayout Layout { get; set; }

    /// <summary>Gets whether the message is currently shown.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the NSS element name emitted by InlineMessage.</summary>
    protected override string ElementName => "InlineMessage";

    /// <summary>Validates the parameters and synchronizes the open state.</summary>
    protected override void OnParametersSet()
    {
        FeedbackParts.Validate(Severity, nameof(Severity));
        if (!Enum.IsDefined(Layout)) throw new ArgumentOutOfRangeException(nameof(Layout));
        open.Sync(IsOpen);
    }

    /// <summary>Gets the caller's declarations and disables pointer interaction during the retained exit.</summary>
    protected override string? ElementStyle => exiting ? Css.Join(Style, "pointer-events: none") : Style;

    /// <summary>Renders the message through a naturally measured Collapsible, retaining its complete height for theme-controlled exit and reversal.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Collapsible>(0);
        builder.AddComponentParameter(1, nameof(Collapsible.As), "InlineMessagePresence");
        builder.AddComponentParameter(2, nameof(Collapsible.IsOpen), open.Value);
        builder.AddComponentParameter(3, nameof(Collapsible.ChildContent), (RenderFragment)(content =>
        {
            exiting = !open.Value;
            base.BuildRenderTree(content);
        }));
        builder.CloseComponent();
    }

    /// <summary>Adds the status role, the severity, the layout and the live politeness.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "status");
        builder.AddAttribute(11, "severity", Vocabulary.Name(Severity));
        builder.AddAttribute(12, "live", FeedbackParts.Politeness(Severity));
        builder.AddAttribute(13, "layout", Layout == InlineMessageLayout.Expanded ? "expanded" : "compact");
        builder.AddAttribute(14, "closable", IsClosable);
        builder.AddAttribute(15, "live-title", Title);
        builder.AddAttribute(17, "exiting", exiting);
        if (exiting)
        {
            builder.AddAttribute(18, "disabled", true);
            builder.AddAttribute(19, "semantic-hidden", true);
        }
    }

    /// <summary>Adds the mark, the body, the actions and the close button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        FeedbackParts.Mark(builder, 1000, "InlineMessageMark", Severity);
        builder.OpenElement(1010, "InlineMessageBody");
        if (Title is not null)
        {
            builder.OpenElement(1011, "InlineMessageTitle");
            builder.AddContent(1012, Title);
            builder.CloseElement();
        }
        if (ChildContent is not null)
        {
            builder.OpenElement(1013, "InlineMessageText");
            builder.AddAttribute(1014, "live-message", true);
            builder.AddContent(1015, ChildContent);
            builder.CloseElement();
        }
        builder.CloseElement();
        if (Actions is not null)
        {
            builder.OpenElement(1020, "InlineMessageActions");
            builder.AddContent(1021, Actions);
            builder.CloseElement();
        }
        if (IsClosable) FeedbackParts.CloseButton(builder, 1030, "InlineMessageClose", this, CloseAsync);
    }

    /// <summary>Hides the message as the close button does and reports the change.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task CloseAsync()
    {
        if (!open.Set(false)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(false);
        await OnClose.InvokeAsync();
    }
}

/// <summary>
/// A message strip across the top of a window or a page about the whole application, such as being offline or an
/// available update. The parts are <c>BannerMark</c> (a mark for the <see cref="Severity" />), <c>BannerMessage</c>
/// (the content between the tags), the <c>BannerAction</c> button (with <see cref="ActionText" />, raising
/// <see cref="OnAction" />) and, with <see cref="Dismissible" />, the <c>BannerClose</c> button. <see cref="IsOpen" />
/// supports <c>@bind-IsOpen</c>. Like <see cref="InlineMessage" /> it is announced when shown, errors at once.
/// </summary>
public sealed class Banner : ContentControl
{
    private readonly ControllableState<bool> open = new(true);

    /// <summary>Gets or sets the importance of the message.</summary>
    [Parameter] public Severity Severity { get; set; }

    /// <summary>Gets or sets the text of the action button, or null for none.</summary>
    [Parameter] public string? ActionText { get; set; }

    /// <summary>Gets or sets the callback invoked when the action button is pressed.</summary>
    [Parameter] public EventCallback OnAction { get; set; }

    /// <summary>Gets or sets whether a close button is shown.</summary>
    [Parameter] public bool Dismissible { get; set; } = true;

    /// <summary>Gets or sets whether the banner is shown; true by default.</summary>
    [Parameter] public bool IsOpen { get; set; } = true;

    /// <summary>Gets or sets the callback invoked when the close button hides the banner.</summary>
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Gets or sets the callback invoked after the close button hides the banner.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Gets whether the banner is currently shown.</summary>
    public bool Opened => open.Value;

    /// <summary>Gets the NSS element name emitted by Banner.</summary>
    protected override string ElementName => "Banner";

    /// <summary>Validates the severity and synchronizes the open state.</summary>
    protected override void OnParametersSet()
    {
        FeedbackParts.Validate(Severity, nameof(Severity));
        open.Sync(IsOpen);
    }

    /// <summary>Renders the banner only while it is open.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (open.Value) base.BuildRenderTree(builder);
    }

    /// <summary>Adds the status role, the severity and the live politeness.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "status");
        builder.AddAttribute(11, "severity", Vocabulary.Name(Severity));
        builder.AddAttribute(12, "live", FeedbackParts.Politeness(Severity));
    }

    /// <summary>Adds the mark, the message, the action and the close button.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        FeedbackParts.Mark(builder, 1000, "BannerMark", Severity);
        builder.OpenElement(1010, "BannerMessage");
        builder.AddAttribute(1011, "live-message", true);
        builder.AddContent(1012, ChildContent);
        builder.CloseElement();
        if (ActionText is not null) FeedbackParts.Button(builder, 1020, "BannerAction", ActionText, null, this, () => OnAction.InvokeAsync());
        if (Dismissible) FeedbackParts.CloseButton(builder, 1030, "BannerClose", this, CloseAsync);
    }

    /// <summary>Hides the banner as the close button does and reports the change.</summary>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task CloseAsync()
    {
        if (!open.Set(false)) return;
        StateHasChanged();
        await IsOpenChanged.InvokeAsync(false);
        await OnClose.InvokeAsync();
    }
}
