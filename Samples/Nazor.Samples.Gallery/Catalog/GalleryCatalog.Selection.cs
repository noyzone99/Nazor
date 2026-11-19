using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Selection" group.
    private static IEnumerable<GalleryEntry> SelectionEntries() =>
    [
        new("checkbox", "CheckBox", "Selection", "Choose yes or no independently, with a mixed state for groups.", typeof(CheckBox), typeof(Examples.Selection.CheckBoxSample)),
        new("switch", "Switch", "Selection", "Toggle a setting with the keyboard or pointer.", typeof(Switch), typeof(Examples.Selection.SwitchSample)),
        new("radio-group", "RadioGroup", "Selection", "Choose one option with arrow keys or pointer input.", typeof(RadioGroup<string>), typeof(Examples.Selection.RadioGroupSample), typeof(RadioButton<string>)),
        new("segmented-control", "SegmentedControl", "Selection", "Switch between modes with a compact group of segments.", typeof(SegmentedControl<string>), typeof(Examples.Selection.SegmentedControlSample), typeof(Segment<string>)),
        new("chip-group", "Chip and ChipGroup", "Selection", "Select a filter chip and show a standalone status chip.", typeof(ChipGroup<string>), typeof(Examples.Selection.ChipGroupSample), typeof(Chip)),
        new("sliders", "Slider and RangeSlider", "Selection", "Adjust one value or choose a lower and upper bound.", typeof(Slider), typeof(Examples.Selection.SlidersSample), typeof(RangeSlider)),
        new("dial-rating", "Dial and Rating", "Selection", "Adjust a value with a dial and choose a star rating.", typeof(Dial), typeof(Examples.Selection.DialRatingSample), typeof(Rating)),
    ];
}
