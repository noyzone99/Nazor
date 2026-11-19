using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Pickers" group.
    private static IEnumerable<GalleryEntry> PickerEntries() =>
    [
        new("date-picker", "DatePicker", "Pickers", "Enter a date or select one from the calendar.", typeof(DatePicker), typeof(Examples.Pickers.DatePickerSample)),
        new("calendar", "Calendar", "Pickers", "Select a date and navigate between months.", typeof(Controls.Calendar), typeof(Examples.Pickers.CalendarSample)),
        new("time-pickers", "TimePicker and DateTimePicker", "Pickers", "Choose a time or combine a date and time in one field.", typeof(TimePicker), typeof(Examples.Pickers.TimePickersSample), typeof(DateTimePicker)),
        new("segmented-pickers", "Segmented date and time pickers", "Pickers", "Choose a date or time through localized segments and confirm or cancel a draft.", typeof(SegmentedDatePicker), typeof(Examples.Pickers.SegmentedPickersSample), typeof(SegmentedTimePicker)),
        new("color-picker", "ColorPicker", "Pickers", "Choose a color with an HSV spectrum, channel sliders, RGB/HSV text, CSS hexadecimal and opacity preview.", typeof(ColorPicker), typeof(Examples.Pickers.ColorPickerSample)),
    ];
}
