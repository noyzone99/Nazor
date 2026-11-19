using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The corner of its positioned container where a <see cref="FloatingActionButton" /> floats.</summary>
public enum FloatingActionButtonPlacement
{
    /// <summary>The bottom corner on the trailing side, written as <c>bottom-end</c>.</summary>
    BottomEnd,
    /// <summary>The bottom corner on the leading side, written as <c>bottom-start</c>.</summary>
    BottomStart,
    /// <summary>The top corner on the trailing side, written as <c>top-end</c>.</summary>
    TopEnd,
    /// <summary>The top corner on the leading side, written as <c>top-start</c>.</summary>
    TopStart,
}

/// <summary>
/// The main action of a view floating above its content, such as Compose in a mail application: an icon
/// (<see cref="ButtonBase.Leading" />) and, when there is content, a label that makes it extended (the
/// <c>extended</c> attribute). It runs <see cref="ButtonBase.OnClick" /> and the optional <see cref="ButtonBase.Command" />;
/// without content the command's text is its accessible name and tooltip instead of a label. The <c>placement</c>
/// attribute (<c>bottom-end</c>, <c>bottom-start</c>, <c>top-end</c>, <c>top-start</c>) tells themes which corner of the
/// nearest positioned container it floats in. It has the button role and the parts of <see cref="ButtonBase" />.
/// </summary>
public sealed class FloatingActionButton : ButtonBase
{
    /// <summary>Gets or sets the corner where the button floats.</summary>
    [Parameter] public FloatingActionButtonPlacement Placement { get; set; }

    /// <summary>Gets the NSS element name emitted by FloatingActionButton.</summary>
    protected override string ElementName => "FloatingActionButton";

    /// <summary>Gets whether a label is shown: with content, or with a command and no icon.</summary>
    protected override bool HasLabel => ChildContent is not null || (Leading is null && Command is not null);

    /// <summary>Validates the parameters.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(Placement)) throw new ArgumentOutOfRangeException(nameof(Placement));
    }

    /// <summary>Adds the button role, the extended state, the placement and the activation.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        base.AddAttributes(builder);
        builder.AddAttribute(30, "role", "button");
        builder.AddAttribute(31, "extended", HasLabel);
        builder.AddAttribute(32, "placement", Placement switch
        {
            FloatingActionButtonPlacement.BottomStart => "bottom-start",
            FloatingActionButtonPlacement.TopEnd => "top-end",
            FloatingActionButtonPlacement.TopStart => "top-start",
            _ => "bottom-end",
        });
        builder.AddAttribute(33, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, ActivateAsync));
        if (Tooltip is null && EffectiveTooltip is null && !HasLabel && Command is not null) builder.AddAttribute(34, "tooltip", Command.Text);
    }

    private Task ActivateAsync(PointerEventArgs e) => IsInactive ? Task.CompletedTask : InvokeActionAsync(e);
}
