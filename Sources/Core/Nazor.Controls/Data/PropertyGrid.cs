using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Nazor.Controls.Primitives;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Controls;

/// <summary>The editor a <see cref="PropertyGrid" /> uses for a property.</summary>
public enum PropertyGridEditor
{
    /// <summary>Chosen from the type: a check box for booleans, a choice for enumerations, a number editor for numbers, text otherwise.</summary>
    Auto,
    /// <summary>A text field.</summary>
    Text,
    /// <summary>A number editor with parsing and stepping with Up and Down.</summary>
    Number,
    /// <summary>A check box.</summary>
    Boolean,
    /// <summary>A drop-down list of <see cref="PropertyGridItem.Choices" /> or of the values of an enumeration.</summary>
    Choice,
    /// <summary>The value as text, without editing.</summary>
    ReadOnly,
    /// <summary>The item's <see cref="PropertyGridItem.EditorTemplate" /> or the grid's <see cref="PropertyGrid.EditorTemplate" />.</summary>
    Custom,
}

/// <summary>How a <see cref="PropertyGrid" /> orders its properties, written as the <c>order</c> attribute.</summary>
public enum PropertyGridOrder
{
    /// <summary>Under collapsible category headers, alphabetically within each category.</summary>
    Categorized,
    /// <summary>In one alphabetical list.</summary>
    Alphabetical,
}

/// <summary>
/// A property shown by a <see cref="PropertyGrid" />: its name, category and description, how its value is read and
/// written, and which editor changes it. <see cref="FromObject" /> describes the public properties of an object.
/// </summary>
public sealed class PropertyGridItem
{
    /// <summary>Gets the stable name of the property.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the name shown and read by assistive technologies; <see cref="Name" /> when null.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the category the property is listed under; a localized "Misc" when null.</summary>
    public string? Category { get; init; }

    /// <summary>Gets the description shown in the description pane when the property is selected.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the type of the value, which chooses the automatic editor.</summary>
    public Type PropertyType { get; init; } = typeof(string);

    /// <summary>Gets the function that reads the current value.</summary>
    public required Func<object?> Getter { get; init; }

    /// <summary>Gets the function that stores a new value, or null for a read-only property.</summary>
    public Action<object?>? Setter { get; init; }

    /// <summary>Gets whether the property cannot be changed even with a setter.</summary>
    public bool ReadOnly { get; init; }

    /// <summary>Gets the editor of the property.</summary>
    public PropertyGridEditor Editor { get; init; }

    /// <summary>Gets the content that edits the property, used by the custom editor.</summary>
    public RenderFragment<PropertyGridEditContext>? EditorTemplate { get; init; }

    /// <summary>Gets the values offered by the choice editor; the values of the enumeration when null.</summary>
    public IReadOnlyList<object?>? Choices { get; init; }

    /// <summary>Gets the choice label formatter; ordinary value text is used when null. Supply labels explicitly for trim-safe enum customization.</summary>
    public Func<object?, string>? ChoiceText { get; init; }

    /// <summary>Gets the smallest value of a number, or null for the limit of its type.</summary>
    public double? Minimum { get; init; }

    /// <summary>Gets the largest value of a number, or null for the limit of its type.</summary>
    public double? Maximum { get; init; }

    /// <summary>Gets the change made to a number by Up and Down.</summary>
    public double Step { get; init; } = 1;

    /// <summary>Gets the name shown.</summary>
    public string Text => DisplayName ?? Name;

    /// <summary>Gets whether the value cannot be changed.</summary>
    public bool IsReadOnly => ReadOnly || Setter is null;

    /// <summary>
    /// Describes the public instance properties of an object that can be read and are browsable, with the
    /// <see cref="DisplayNameAttribute" />, <see cref="DescriptionAttribute" />, <see cref="CategoryAttribute" />,
    /// <see cref="ReadOnlyAttribute" /> and <see cref="BrowsableAttribute" /> of <c>System.ComponentModel</c> when present.
    /// </summary>
    /// <param name="target">The object to inspect.</param>
    /// <returns>The properties, in declaration order.</returns>
    /// <remarks>Reflection requires preserved public properties and enum label attributes. Prefer explicit Items for trimming and Native AOT.</remarks>
    [RequiresUnreferencedCode("The target's public properties and enum label attributes must be preserved. Use explicit PropertyGrid.Items for trimming and Native AOT.")]
    public static IReadOnlyList<PropertyGridItem> FromObject(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var result = new List<PropertyGridItem>();
        foreach (var property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetMethod?.IsPublic != true || property.GetIndexParameters().Length > 0) continue;
            if (property.GetCustomAttribute<BrowsableAttribute>()?.Browsable == false) continue;
            var writable = property.CanWrite && property.SetMethod?.IsPublic == true;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var names = type.IsEnum ? Enum.GetNames(type) : null;
            var labels = names?.ToDictionary(name => name, name =>
            {
                var field = type.GetField(name)!;
                return field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? field.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? name;
            });
            result.Add(new PropertyGridItem
            {
                Name = property.Name,
                DisplayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName is { Length: > 0 } display ? display : null,
                Category = property.GetCustomAttribute<CategoryAttribute>()?.Category,
                Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description is { Length: > 0 } description ? description : null,
                PropertyType = property.PropertyType,
                Getter = () => property.GetValue(target),
                Setter = writable ? value => property.SetValue(target, value) : null,
                ReadOnly = property.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly == true,
                Choices = names?.Select(name => (object?)Enum.Parse(type, name)).ToArray(),
                ChoiceText = labels is null ? null : value => value is null ? "" : labels.GetValueOrDefault(value.ToString()!, value.ToString()!),
            });
        }
        return result;
    }

    internal PropertyGridEditor ResolvedEditor
    {
        get
        {
            if (Editor != PropertyGridEditor.Auto) return Editor;
            var type = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
            if (type == typeof(bool)) return PropertyGridEditor.Boolean;
            if (type.IsEnum || Choices is not null) return PropertyGridEditor.Choice;
            if (PropertyGrid.IsNumber(type)) return PropertyGridEditor.Number;
            if (type == typeof(string) || type == typeof(char) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
                return PropertyGridEditor.Text;
            return EditorTemplate is not null ? PropertyGridEditor.Custom : PropertyGridEditor.ReadOnly;
        }
    }
}

/// <summary>The property edited by a custom editor of a <see cref="PropertyGrid" />.</summary>
public sealed class PropertyGridEditContext
{
    private readonly Func<object?, Task> set;

    internal PropertyGridEditContext(PropertyGridItem item, string labelId, Func<object?, Task> set)
    {
        Item = item;
        LabelId = labelId;
        this.set = set;
    }

    /// <summary>Gets the edited property.</summary>
    public PropertyGridItem Item { get; }

    /// <summary>Gets the current value.</summary>
    public object? Value => Item.Getter();

    /// <summary>Gets the identifier of the property name, for the editor's <c>LabeledBy</c>.</summary>
    public string LabelId { get; }

    /// <summary>Stores a new value and reports the change.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public Task SetValueAsync(object? value) => set(value);
}

/// <summary>A property value changed by the user in a <see cref="PropertyGrid" />.</summary>
/// <param name="Item">The changed property.</param>
/// <param name="OldValue">The value before the change.</param>
/// <param name="NewValue">The stored value.</param>
public sealed record PropertyGridChange(PropertyGridItem Item, object? OldValue, object? NewValue);

/// <summary>
/// Shows and edits the properties of an object: from reflection over <see cref="Target" />, or from explicit
/// <see cref="Items" />. Properties are listed under collapsible categories or alphabetically (<see cref="Order" />),
/// filtered by a search field, and edited in place: text fields for strings, number editors for numbers, check boxes
/// for booleans, drop-down lists for enumerations, and custom editors through <see cref="EditorTemplate" />. The
/// description pane shows the selected property's description. The parts are <c>PropertyGridToolbar</c>,
/// <c>PropertyGridOrder</c> with <c>PropertyGridOrderOption</c> (<c>checked</c>), a <c>TextField</c> for the search,
/// <c>PropertyGridList</c>, <c>PropertyGridCategory</c> (<c>expanded</c>) with <c>PropertyGridCategoryHeader</c>,
/// <c>PropertyGridCategoryToggle</c> and <c>PropertyGridCategoryBody</c>, <c>PropertyGridRow</c> (<c>selected</c>,
/// <c>readonly</c>, <c>modified</c>), <c>PropertyGridName</c>, <c>PropertyGridValue</c>, <c>PropertyGridNumber</c>,
/// <c>PropertyGridReadOnlyValue</c>, <c>PropertyGridEmpty</c>, <c>PropertyGridDescription</c>,
/// <c>PropertyGridDescriptionTitle</c> and <c>PropertyGridDescriptionText</c>.
/// </summary>
public sealed class PropertyGrid : Control
{
    private readonly string idPrefix = UniqueId.Create("property");
    private readonly ControllableState<PropertyGridOrder> order = new(PropertyGridOrder.Categorized);
    private readonly ControllableState<string> search = new("");
    private readonly ControllableState<string?> selected = new(null);
    private readonly HashSet<string> collapsed = [];
    private readonly Dictionary<string, string> numberTexts = [];
    private readonly HashSet<string> invalid = [];
    private IReadOnlyList<PropertyGridItem> items = [];
    private object? reflected;
    private object? target;
    private Func<IReadOnlyList<PropertyGridItem>>? describeTarget;

    /// <summary>Gets or sets the object whose public properties are shown, when <see cref="Items" /> is null.</summary>
    /// <remarks>The setter requires preserved reflection metadata. Use explicit Items and ChoiceText formatters for trimming and Native AOT.</remarks>
    [Parameter]
    public object? Target
    {
        get => target;
        [RequiresUnreferencedCode("The target's public properties and enum label attributes must be preserved. Use explicit PropertyGrid.Items for trimming and Native AOT.")]
        set
        {
            if (ReferenceEquals(target, value)) return;
            target = value;
            describeTarget = value is null ? null : () => PropertyGridItem.FromObject(value);
        }
    }

    /// <summary>Gets or sets the properties shown, replacing reflection over <see cref="Target" />.</summary>
    [Parameter] public IReadOnlyList<PropertyGridItem>? Items { get; set; }

    /// <summary>Gets or sets how the properties are ordered.</summary>
    [Parameter] public PropertyGridOrder Order { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the order; supports <c>@bind-Order</c>.</summary>
    [Parameter] public EventCallback<PropertyGridOrder> OrderChanged { get; set; }

    /// <summary>Gets or sets the search text that filters properties by name, category and description.</summary>
    [Parameter] public string? Search { get; set; }

    /// <summary>Gets or sets the callback invoked when the user changes the search; supports <c>@bind-Search</c>.</summary>
    [Parameter] public EventCallback<string> SearchChanged { get; set; }

    /// <summary>Gets or sets the name of the selected property.</summary>
    [Parameter] public string? SelectedName { get; set; }

    /// <summary>Gets or sets the callback invoked when the user selects a property; supports <c>@bind-SelectedName</c>.</summary>
    [Parameter] public EventCallback<string?> SelectedNameChanged { get; set; }

    /// <summary>Gets or sets whether the toolbar with the order choice and the search field is shown.</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;

    /// <summary>Gets or sets whether the description pane is shown.</summary>
    [Parameter] public bool ShowDescription { get; set; } = true;

    /// <summary>Gets or sets whether every property is shown without editing.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Gets or sets the editor of custom properties and of types without a built-in editor.</summary>
    [Parameter] public RenderFragment<PropertyGridEditContext>? EditorTemplate { get; set; }

    /// <summary>Gets or sets the callback invoked after the user changes a value.</summary>
    [Parameter] public EventCallback<PropertyGridChange> OnPropertyChanged { get; set; }

    /// <summary>Gets the properties shown, before searching.</summary>
    public IReadOnlyList<PropertyGridItem> Properties => items;

    /// <summary>Gets the NSS element name emitted by PropertyGrid.</summary>
    protected override string ElementName => "PropertyGrid";

    /// <summary>Validates the parameters and describes the target.</summary>
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Order)) throw new ArgumentOutOfRangeException(nameof(Order));
        order.Sync(Order);
        search.Sync(Search ?? "");
        selected.Sync(SelectedName);
        if (Items is not null) items = Items;
        else if (!ReferenceEquals(reflected, Target) || Target is null)
        {
            items = describeTarget?.Invoke() ?? [];
            numberTexts.Clear();
            invalid.Clear();
        }
        reflected = Target;
    }

    internal static bool IsNumber(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint)
        || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    private static string CategoryOf(PropertyGridItem item) => string.IsNullOrWhiteSpace(item.Category) ? ControlStrings.Get("Miscellaneous") : item.Category;

    private string NameId(PropertyGridItem item) => idPrefix + "-" + item.Name;

    private List<PropertyGridItem> Visible()
    {
        var query = search.Value.Trim();
        return [.. items.Where(item => query.Length == 0
            || item.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || CategoryOf(item).Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || item.Description?.Contains(query, StringComparison.CurrentCultureIgnoreCase) == true)
            .OrderBy(item => item.Text, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>Adds the group role and the order attribute.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddAttributes(RenderTreeBuilder builder)
    {
        builder.AddAttribute(10, "role", "group");
        if (AccessibleName is null && LabeledBy is null) builder.AddAttribute(11, "accessible-name", ControlStrings.Get("Properties"));
        builder.AddAttribute(12, "order", order.Value == PropertyGridOrder.Alphabetical ? "alphabetical" : "categorized");
        builder.AddAttribute(13, "readonly", ReadOnly);
    }

    /// <summary>Adds the toolbar, the properties and the description pane.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    protected override void AddContent(RenderTreeBuilder builder)
    {
        var visible = Visible();
        if (ShowToolbar)
        {
            builder.OpenElement(1000, "PropertyGridToolbar");
            builder.OpenElement(1001, "PropertyGridOrder");
            builder.AddAttribute(1002, "role", "radiogroup");
            builder.AddAttribute(1003, "accessible-name", ControlStrings.Get("Categorized") + ", " + ControlStrings.Get("Alphabetical"));
            AddOrderOption(builder, 1010, PropertyGridOrder.Categorized, "Categorized");
            AddOrderOption(builder, 1020, PropertyGridOrder.Alphabetical, "Alphabetical");
            builder.CloseElement();
            builder.OpenComponent<TextField>(1030);
            builder.AddComponentParameter(1031, nameof(TextField.Value), search.Value);
            builder.AddComponentParameter(1032, nameof(TextField.ValueChanged), EventCallback.Factory.Create<string>(this, SearchAsync));
            builder.AddComponentParameter(1033, nameof(TextField.Placeholder), ControlStrings.Get("SearchProperties"));
            builder.AddComponentParameter(1034, nameof(TextField.AccessibleName), ControlStrings.Get("SearchProperties"));
            builder.AddComponentParameter(1035, nameof(TextField.Clearable), true);
            builder.AddComponentParameter(1036, nameof(TextField.Class), "PropertyGridSearch");
            builder.CloseComponent();
            builder.CloseElement();
        }
        builder.OpenElement(1100, "PropertyGridList");
        if (visible.Count == 0)
        {
            builder.OpenElement(1101, "PropertyGridEmpty");
            builder.AddAttribute(1102, "role", "status");
            builder.AddContent(1103, ControlStrings.Get("NoResults"));
            builder.CloseElement();
        }
        else if (order.Value == PropertyGridOrder.Alphabetical)
            foreach (var item in visible) AddRow(builder, item);
        else
            foreach (var group in visible.GroupBy(CategoryOf).OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
                AddCategory(builder, group.Key, [.. group]);
        builder.CloseElement();
        if (ShowDescription)
        {
            var current = items.FirstOrDefault(i => i.Name == selected.Value);
            builder.OpenElement(1200, "PropertyGridDescription");
            builder.AddAttribute(1201, "role", "status");
            builder.AddAttribute(1202, "empty", current is null);
            if (current is not null)
            {
                builder.OpenElement(1203, "PropertyGridDescriptionTitle");
                builder.AddContent(1204, current.Text);
                builder.CloseElement();
                builder.OpenElement(1205, "PropertyGridDescriptionText");
                builder.AddContent(1206, current.Description);
                builder.CloseElement();
            }
            builder.CloseElement();
        }
    }

    private void AddOrderOption(RenderTreeBuilder builder, int sequence, PropertyGridOrder value, string text)
    {
        var isChecked = order.Value == value;
        builder.OpenElement(sequence, "PropertyGridOrderOption");
        builder.AddAttribute(sequence + 1, "role", "radio");
        builder.AddAttribute(sequence + 2, "checked", isChecked);
        builder.AddAttribute(sequence + 3, "focusable", true);
        builder.AddAttribute(sequence + 4, "tabindex", isChecked ? 0 : -1);
        builder.AddAttribute(sequence + 5, "value", value == PropertyGridOrder.Alphabetical ? "alphabetical" : "categorized");
        builder.AddAttribute(sequence + 6, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => SetOrderAsync(value)));
        builder.AddContent(sequence + 7, ControlStrings.Get(text));
        builder.CloseElement();
    }

    private void AddCategory(RenderTreeBuilder builder, string category, List<PropertyGridItem> members)
    {
        var open = !collapsed.Contains(category) || search.Value.Trim().Length > 0;
        builder.OpenElement(0, "PropertyGridCategory");
        builder.SetKey("category:" + category);
        builder.AddAttribute(1, "role", "group");
        builder.AddAttribute(2, "accessible-name", category);
        builder.AddAttribute(3, "expanded", open);
        builder.OpenElement(4, "PropertyGridCategoryHeader");
        builder.AddAttribute(5, "role", "button");
        builder.AddAttribute(6, "expanded", open);
        builder.AddAttribute(7, "focusable", true);
        builder.AddAttribute(8, "__default_onclick", EventCallback.Factory.Create<PointerEventArgs>(this, () => ToggleCategory(category)));
        builder.AddAttribute(9, "__internal_onkeydown#property", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => CategoryKey(category, open, e)));
        builder.OpenElement(10, "PropertyGridCategoryToggle");
        builder.AddAttribute(11, "glyph", open ? "chevron-down" : "chevron-right");
        builder.AddAttribute(12, "semantic-hidden", true);
        builder.CloseElement();
        builder.OpenElement(13, "PropertyGridCategoryTitle");
        builder.AddContent(14, category);
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenComponent<Collapsible>(20);
        builder.AddComponentParameter(21, nameof(Collapsible.As), "PropertyGridCategoryBody");
        builder.AddComponentParameter(22, nameof(Collapsible.IsOpen), open);
        builder.AddComponentParameter(23, nameof(Collapsible.ChildContent), (RenderFragment)(b =>
        {
            foreach (var item in members) AddRow(b, item);
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    private void CategoryKey(string category, bool open, KeyboardEventArgs e)
    {
        if (e.Handled || e.Modifiers != KeyModifiers.None) return;
        if (e.Key == "ArrowRight" && !open || e.Key == "ArrowLeft" && open)
        {
            e.Handled = true;
            ToggleCategory(category);
        }
    }

    private void ToggleCategory(string category)
    {
        if (!collapsed.Remove(category)) collapsed.Add(category);
        StateHasChanged();
    }

    private void AddRow(RenderTreeBuilder builder, PropertyGridItem item)
    {
        var readOnly = ReadOnly || item.IsReadOnly;
        builder.OpenElement(100, "PropertyGridRow");
        builder.SetKey("property:" + item.Name);
        builder.AddAttribute(101, "selected", selected.Value == item.Name);
        builder.AddAttribute(102, "readonly", readOnly);
        builder.AddAttribute(103, "invalid", invalid.Contains(item.Name));
        builder.AddAttribute(104, "__internal_onfocusin#property", EventCallback.Factory.Create<FocusEventArgs>(this, () => SelectAsync(item.Name)));
        builder.AddAttribute(105, "__internal_onpointerdown#property", EventCallback.Factory.Create<PointerEventArgs>(this, () => SelectAsync(item.Name)));
        builder.OpenElement(106, "PropertyGridName");
        builder.AddAttribute(107, "id", NameId(item));
        builder.AddAttribute(108, "tooltip", item.Description);
        builder.AddContent(109, item.Text);
        builder.CloseElement();
        builder.OpenElement(110, "PropertyGridValue");
        AddEditor(builder, item, readOnly);
        builder.CloseElement();
        builder.CloseElement();
    }

    private void AddEditor(RenderTreeBuilder builder, PropertyGridItem item, bool readOnly)
    {
        var value = item.Getter();
        var editor = item.ResolvedEditor;
        var labelId = NameId(item);
        var template = item.EditorTemplate ?? EditorTemplate;
        if (editor == PropertyGridEditor.Custom && template is null) editor = PropertyGridEditor.ReadOnly;
        if (readOnly && editor is PropertyGridEditor.Text or PropertyGridEditor.Number or PropertyGridEditor.Custom) editor = PropertyGridEditor.ReadOnly;
        switch (editor)
        {
            case PropertyGridEditor.Boolean:
                builder.OpenComponent<CheckBox>(200);
                builder.AddComponentParameter(201, nameof(CheckBox.IsChecked), value is true);
                builder.AddComponentParameter(202, nameof(CheckBox.Disabled), readOnly);
                builder.AddComponentParameter(203, nameof(CheckBox.LabeledBy), labelId);
                builder.AddComponentParameter(204, nameof(CheckBox.IsCheckedChanged), EventCallback.Factory.Create<bool>(this, v => SetValueAsync(item, v)));
                builder.CloseComponent();
                break;
            case PropertyGridEditor.Choice:
                var type = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;
                IReadOnlyList<object?> choices = item.Choices ?? (type.IsEnum ? [.. Enum.GetNames(type).Select(name => (object?)Enum.Parse(type, name))] : []);
                builder.OpenComponent<Select<object?>>(210);
                builder.AddComponentParameter(211, nameof(Select<object?>.Value), value);
                builder.AddComponentParameter(212, nameof(Select<object?>.Items), choices);
                builder.AddComponentParameter(213, nameof(Select<object?>.ItemText), item.ChoiceText ?? (Func<object?, string>)ChoiceText);
                builder.AddComponentParameter(214, nameof(Select<object?>.Disabled), readOnly);
                builder.AddComponentParameter(215, nameof(Select<object?>.LabeledBy), labelId);
                builder.AddComponentParameter(216, nameof(Select<object?>.Appearance), FieldAppearance.Plain);
                builder.AddComponentParameter(217, nameof(Select<object?>.ValueChanged), EventCallback.Factory.Create<object?>(this, v => SetValueAsync(item, v)));
                builder.CloseComponent();
                break;
            case PropertyGridEditor.Number:
                AddNumber(builder, item, value, labelId);
                break;
            case PropertyGridEditor.Text:
                builder.OpenComponent<TextField>(230);
                builder.AddComponentParameter(231, nameof(TextField.Value), Convert.ToString(value, CultureInfo.CurrentCulture) ?? "");
                builder.AddComponentParameter(232, nameof(TextField.Appearance), FieldAppearance.Plain);
                builder.AddComponentParameter(233, nameof(TextField.LabeledBy), labelId);
                builder.AddComponentParameter(234, nameof(TextField.Invalid), invalid.Contains(item.Name));
                builder.AddComponentParameter(235, nameof(TextField.OnChange), EventCallback.Factory.Create<ChangeEventArgs>(this, e => TextCommittedAsync(item, e.Value as string ?? "")));
                builder.CloseComponent();
                break;
            case PropertyGridEditor.Custom:
                builder.AddContent(240, template!(new PropertyGridEditContext(item, labelId, v => SetValueAsync(item, v))));
                break;
            default:
                builder.OpenElement(250, "PropertyGridReadOnlyValue");
                builder.AddAttribute(251, "role", "text");
                builder.AddAttribute(252, "labeled-by", labelId);
                builder.AddContent(253, value is bool b ? (b ? "✓" : "—") : Convert.ToString(value, CultureInfo.CurrentCulture));
                builder.CloseElement();
                break;
        }
    }

    private static string ChoiceText(object? value)
    {
        if (value is null) return "";
        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
    }

    private (double Minimum, double Maximum) Limits(PropertyGridItem item)
    {
        var type = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;
        (double low, double high) = type switch
        {
            _ when type == typeof(byte) => (byte.MinValue, byte.MaxValue),
            _ when type == typeof(sbyte) => (sbyte.MinValue, sbyte.MaxValue),
            _ when type == typeof(short) => (short.MinValue, short.MaxValue),
            _ when type == typeof(ushort) => (ushort.MinValue, ushort.MaxValue),
            _ when type == typeof(int) => (int.MinValue, int.MaxValue),
            _ when type == typeof(uint) => (uint.MinValue, uint.MaxValue),
            _ when type == typeof(long) => (long.MinValue, long.MaxValue),
            _ when type == typeof(ulong) => (0, ulong.MaxValue),
            _ when type == typeof(float) => (float.MinValue, float.MaxValue),
            _ when type == typeof(decimal) => ((double)decimal.MinValue, (double)decimal.MaxValue),
            _ => (double.MinValue, double.MaxValue),
        };
        return (item.Minimum ?? low, item.Maximum ?? high);
    }

    private void AddNumber(RenderTreeBuilder builder, PropertyGridItem item, object? value, string labelId)
    {
        var (minimum, maximum) = Limits(item);
        var number = value is null ? 0 : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        var text = numberTexts.TryGetValue(item.Name, out var typed) ? typed : Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
        builder.OpenComponent<EditableText>(220);
        builder.AddComponentParameter(221, nameof(EditableText.As), "PropertyGridNumber");
        builder.AddComponentParameter(222, nameof(EditableText.Value), text);
        builder.AddComponentParameter(223, nameof(EditableText.Role), "spinbutton");
        builder.AddComponentParameter(224, nameof(EditableText.InputMode), "number");
        builder.AddComponentParameter(225, nameof(EditableText.Invalid), invalid.Contains(item.Name));
        builder.AddComponentParameter(226, nameof(EditableText.LabeledBy), labelId);
        builder.AddComponentParameter(227, nameof(EditableText.Spin), (SpinRange?)new SpinRange(Math.Clamp(number, minimum, maximum), minimum, maximum, item.Step, item.Step * 10));
        builder.AddComponentParameter(228, nameof(EditableText.ValueChanged), EventCallback.Factory.Create<string>(this, t => numberTexts[item.Name] = t));
        builder.AddComponentParameter(229, nameof(EditableText.OnInput), EventCallback.Factory.Create<ChangeEventArgs>(this, e => e.Value is double stepped ? NumberCommittedAsync(item, stepped) : Task.CompletedTask));
        builder.AddComponentParameter(230, nameof(EditableText.OnChange), EventCallback.Factory.Create<ChangeEventArgs>(this, e => NumberTextCommittedAsync(item, e.Value as string ?? text)));
        builder.CloseComponent();
    }

    private Task NumberTextCommittedAsync(PropertyGridItem item, string text)
    {
        if (!double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out var number))
        {
            numberTexts[item.Name] = text;
            invalid.Add(item.Name);
            StateHasChanged();
            return Task.CompletedTask;
        }
        return NumberCommittedAsync(item, number);
    }

    private Task NumberCommittedAsync(PropertyGridItem item, double number)
    {
        var (minimum, maximum) = Limits(item);
        var type = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;
        if (!double.IsFinite(number) || number < minimum || number > maximum || IsInteger(type) && Math.Abs(number - Math.Round(number)) > 1e-9)
        {
            invalid.Add(item.Name);
            StateHasChanged();
            return Task.CompletedTask;
        }
        numberTexts.Remove(item.Name);
        return SetValueAsync(item, Convert.ChangeType(type == typeof(decimal) ? (decimal)number : number, type, CultureInfo.InvariantCulture));
    }

    private static bool IsInteger(Type type) => type != typeof(float) && type != typeof(double) && type != typeof(decimal);

    private Task TextCommittedAsync(PropertyGridItem item, string text)
    {
        var type = Nullable.GetUnderlyingType(item.PropertyType) ?? item.PropertyType;
        object? value;
        try
        {
            value = type == typeof(string) ? text
                : text.Length == 0 && (!item.PropertyType.IsValueType || Nullable.GetUnderlyingType(item.PropertyType) is not null) ? null
                : type == typeof(Guid) ? Guid.Parse(text)
                : type == typeof(TimeSpan) ? TimeSpan.Parse(text, CultureInfo.CurrentCulture)
                : type == typeof(DateTimeOffset) ? DateTimeOffset.Parse(text, CultureInfo.CurrentCulture)
                : Convert.ChangeType(text, type, CultureInfo.CurrentCulture);
        }
        catch (Exception error) when (error is FormatException or InvalidCastException or OverflowException)
        {
            invalid.Add(item.Name);
            StateHasChanged();
            return Task.CompletedTask;
        }
        return SetValueAsync(item, value);
    }

    /// <summary>Stores a new value in a property as an editor does and reports the change.</summary>
    /// <param name="item">The property.</param>
    /// <param name="value">The new value.</param>
    /// <returns>A task that completes when the change has been reported.</returns>
    public async Task SetValueAsync(PropertyGridItem item, object? value)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (ReadOnly || item.IsReadOnly) return;
        invalid.Remove(item.Name);
        var old = item.Getter();
        if (Equals(old, value))
        {
            StateHasChanged();
            return;
        }
        item.Setter!(value);
        StateHasChanged();
        await OnPropertyChanged.InvokeAsync(new PropertyGridChange(item, old, item.Getter()));
        await Events.RaiseChangeAsync(Element, item.Name);
    }

    private async Task SelectAsync(string name)
    {
        if (!selected.Set(name)) return;
        StateHasChanged();
        await SelectedNameChanged.InvokeAsync(name);
    }

    private async Task SearchAsync(string text)
    {
        if (!search.Set(text)) return;
        StateHasChanged();
        await SearchChanged.InvokeAsync(text);
    }

    private async Task SetOrderAsync(PropertyGridOrder value)
    {
        if (!order.Set(value)) return;
        StateHasChanged();
        await OrderChanged.InvokeAsync(value);
    }
}
