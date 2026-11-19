using System.Globalization;
using Nazor.Controls.Primitives;
using StyleColor = Nazor.Style.Color;

namespace Nazor.Controls.Tests.Pickers.Fixtures;

public sealed class ColorPickerProbe : ComponentBase
{
    public StyleColor Value { get; set; } = new(51, 102, 153, 128);
    public bool Controlled { get; set; } = true;
    public bool Alpha { get; set; } = true;
    public bool Disabled { get; set; }
    public bool ReadOnly { get; set; }
    public bool InField { get; set; }
    public bool Visuals { get; set; } = true;
    public bool Inputs { get; set; } = true;
    public bool Hex { get; set; } = true;
    public ColorRepresentation Representation { get; set; }
    public ColorSpectrumComponents Components { get; set; } = ColorSpectrumComponents.HueSaturation;
    public CultureInfo? Culture { get; set; }
    public ColorPicker Picker { get; private set; } = null!;
    public List<StyleColor> Changes { get; } = [];
    public List<StyleColor> Commits { get; } = [];
    public void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (InField)
        {
            builder.OpenComponent<Field>(0);
            builder.AddComponentParameter(1, nameof(Field.LabelText), "Paint color");
            builder.AddComponentParameter(2, nameof(Field.Disabled), Disabled);
            builder.AddComponentParameter(3, nameof(Field.ChildContent), (RenderFragment)PickerContent);
            builder.CloseComponent();
        }
        else PickerContent(builder);
    }

    private void PickerContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ColorPicker>(0);
        builder.AddComponentParameter(1, nameof(ColorPicker.Color), Value);
        builder.AddComponentParameter(2, nameof(ColorPicker.ColorChanged), EventCallback.Factory.Create<StyleColor>(this, next => { if (Controlled) Value = next; Changes.Add(next); }));
        builder.AddComponentParameter(3, nameof(ColorPicker.OnChange), EventCallback.Factory.Create<StyleColor>(this, value => Commits.Add(value)));
        builder.AddComponentParameter(4, nameof(ColorPicker.IsAlphaEnabled), Alpha);
        builder.AddComponentParameter(5, nameof(ColorPicker.Disabled), InField ? false : Disabled);
        builder.AddComponentParameter(6, nameof(ColorPicker.ReadOnly), ReadOnly);
        builder.AddComponentParameter(7, nameof(ColorPicker.Representation), Representation);
        builder.AddComponentParameter(8, nameof(ColorPicker.RepresentationChanged), EventCallback.Factory.Create<ColorRepresentation>(this, value => Representation = value));
        builder.AddComponentParameter(9, nameof(ColorPicker.SpectrumComponents), Components);
        builder.AddComponentParameter(10, nameof(ColorPicker.IsColorSpectrumVisible), Visuals);
        builder.AddComponentParameter(11, nameof(ColorPicker.IsColorSliderVisible), Visuals);
        builder.AddComponentParameter(12, nameof(ColorPicker.IsAlphaSliderVisible), Visuals);
        builder.AddComponentParameter(13, nameof(ColorPicker.IsColorPreviewVisible), Visuals);
        builder.AddComponentParameter(14, nameof(ColorPicker.IsColorChannelTextInputVisible), Inputs);
        builder.AddComponentParameter(15, nameof(ColorPicker.IsAlphaTextInputVisible), Inputs);
        builder.AddComponentParameter(16, nameof(ColorPicker.IsHexInputVisible), Hex);
        builder.AddComponentParameter(17, nameof(ColorPicker.Culture), Culture);
        builder.AddComponentReferenceCapture(18, component => Picker = (ColorPicker)component);
        builder.CloseComponent();
    }
}
