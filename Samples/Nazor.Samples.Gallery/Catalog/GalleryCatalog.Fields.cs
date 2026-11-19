using Nazor.Controls;

namespace Nazor.Samples.Gallery;

public static partial class GalleryCatalog
{
    // Pages of the "Fields" group.
    private static IEnumerable<GalleryEntry> FieldEntries() =>
    [
        new("field", "Field and Label", "Fields", "Connect a control with its label, description and error.", typeof(Field), typeof(Examples.Fields.FieldSample), typeof(Label)),
        new("form-layout", "Form and FormLayout", "Fields", "Submit with Enter and align labels in a column.", typeof(FormLayout), typeof(Examples.Fields.FormLayoutSample), typeof(Form)),
        new("text-field", "TextField", "Fields", "Edit one line of text with prefix, suffix, clear button and counter.", typeof(TextField), typeof(Examples.Fields.TextFieldSample)),
        new("text-area", "TextArea", "Fields", "Edit several lines of text, with a counter and a resize handle.", typeof(TextArea), typeof(Examples.Fields.TextAreaSample)),
        new("select", "Select", "Fields", "Choose one value from a list, with groups and keyboard selection.", typeof(Select<string>), typeof(Examples.Fields.SelectSample), typeof(SelectItem<string>)),
        new("number-field", "NumberField", "Fields", "Edit a formatted value with bounds and step buttons.", typeof(NumberField), typeof(Examples.Fields.NumberFieldSample)),
        new("password-pin", "PasswordField and PinField", "Fields", "Enter a masked password or a fixed-length verification code.", typeof(PasswordField), typeof(Examples.Fields.PasswordPinSample), typeof(PinField)),
        new("masked-shortcut", "MaskedField and ShortcutField", "Fields", "Constrain text to a pattern and record a keyboard shortcut.", typeof(MaskedField), typeof(Examples.Fields.MaskedShortcutSample), typeof(ShortcutField)),
        new("search-combo", "SearchField and ComboBox", "Fields", "Submit a search with suggestions or choose a value from an editable list.", typeof(SearchField<string>), typeof(Examples.Fields.SearchComboSample), typeof(ComboBox<string>)),
        new("search-suggestions", "Search suggestions", "Fields", "Render typed suggestions, submit queries separately from choices, and cancel asynchronous requests.", typeof(SearchField<string>), typeof(Examples.Fields.SearchSuggestionsSample)),
        new("file-token", "FileField and TokenField", "Fields", "Choose native single/multiple files or a folder, filter file types, and edit tags.", typeof(FileField), typeof(Examples.Fields.FileTokenSample), typeof(TokenField<string>)),
    ];
}
