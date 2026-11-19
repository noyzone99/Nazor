using Nazor.Accessibility;
using Nazor.Controls;
using Nazor.Controls.Primitives;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class SemanticControlsTests : IDisposable
{
    private readonly TestHost host = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
        UseSystemFonts = false, DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly SemanticTree semantics;

    public SemanticControlsTests()
    {
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(600, 600) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene);
        semantics = new SemanticTree(host.Renderer, scene, input);
    }

    public void Dispose() { semantics.Dispose(); input.Dispose(); scene.Dispose(); host.Dispose(); graphics.Dispose(); }

    [Fact]
    public async Task Base_dialog_uses_caller_owned_heading_and_description_without_convenience_parts()
    {
        var root=host.Render<Dialog>(ParameterView.FromDictionary(new Dictionary<string,object?>
        {
            ["IsOpen"]=true,["LabeledBy"]="plain-title",["DescribedBy"]="plain-description",
            ["ChildContent"]=(RenderFragment)(b=>
            {
                b.OpenComponent<Heading>(0); b.AddComponentParameter(1,"Id","plain-title"); b.AddComponentParameter(2,"ChildContent",Text("Review")); b.CloseComponent();
                b.OpenComponent<TextBlock>(3); b.AddComponentParameter(4,"Id","plain-description"); b.AddComponentParameter(5,"ChildContent",Text("Check the content")); b.CloseComponent();
            }),
        }));
        semantics.Update(); var dialog=Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Dialog);
        Assert.Equal("Review",dialog.Name); Assert.Equal("Check the content",dialog.Description);
        await ((Dialog)root.Component).CloseAsync(); semantics.Update();
        Assert.DoesNotContain(semantics.Descendants(),node=>node.Role==SemanticRole.Dialog);
    }

    [Fact]
    public void Splitter_exposes_an_adjustable_range_and_readonly_numeric_inputs_reject_range_writes()
    {
        var root = host.Render<ResizeFixture>();
        semantics.Update(); var splitter = Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Splitter);
        Assert.Equal("Pane width",splitter.Name); Assert.NotNull(splitter.Range); Assert.False(splitter.Range.IsReadOnly);
        Assert.True(semantics.SetRangeValue(splitter,250)); semantics.Update(); Assert.Equal(250,splitter.Range.Value);
        Assert.Equal(250.0,root.Single("ResizeHandle").Attributes["range-value"]);
        host.Render<NumberField>(ParameterView.FromDictionary(new Dictionary<string,object?> { ["Value"]=10.0,["ReadOnly"]=true }));
        semantics.Update(); var editor = Assert.Single(semantics.Descendants(),node=>node.Element?.Name=="NumberFieldInput");
        Assert.NotNull(editor.Range); Assert.True(editor.Range.IsReadOnly);
        Assert.False(semantics.SetRangeValue(editor,20)); Assert.Equal("10",editor.Value);
    }

    [Fact]
    public void Ordered_lists_refresh_numbers_and_keep_nested_lists_independent()
    {
        var root = host.Render<ListFixture>();
        var fixture = (ListFixture)root.Component;
        semantics.Update();
        Assert.Equal(new[] { "3.", "8.", "◦", "9." }, root.Elements("ListItemMarker").Select(x => x.Text()).ToArray());
        var lists = semantics.Descendants().Where(x => x.Role == SemanticRole.DocumentList).ToArray();
        Assert.Equal(2, lists.Length);
        Assert.Equal(3, lists[0].Children.Count);
        Assert.All(lists[0].Children, item => Assert.Equal(3, item.SizeOfSet));
        Assert.Equal(1, Assert.Single(lists[1].Children).SizeOfSet);
        fixture.Reverse();
        semantics.Update();
        Assert.Equal(new[] { "3.", "8.", "◦", "7." }, root.Elements("ListItemMarker").Select(x => x.Text()).ToArray());
        Assert.All(root.Elements("ListItem"), item => Assert.False(item.Attributes.ContainsKey("focusable")));
    }

    [Fact]
    public async Task Fields_associate_labels_help_errors_and_inherited_disabled_state()
    {
        var root = host.Render<FieldFixture>();
        var fixture = (FieldFixture)root.Component;
        semantics.Update();
        var editor = Assert.Single(semantics.Descendants(), x => x.Role == SemanticRole.TextBox);
        Assert.Equal("Account", editor.Name);
        Assert.Equal("Use your name", editor.Description);
        Assert.True(editor.IsRequired);
        Assert.False(editor.IsInvalid);
        var label = root.Elements("Label").Single(x => x.Text().StartsWith("Account", StringComparison.Ordinal));
        await host.ClickAsync(label);
        Assert.Same(root.Single("TextFieldInput"), input.Focused);
        var changes = new System.Collections.Generic.List<SemanticProperty>();
        semantics.PropertyChanged += (node, property, _, _) => { if (node == editor) changes.Add(property); };
        fixture.Change(invalid: true, disabled: true);
        semantics.Update();
        Assert.Equal("Use your name Already taken", editor.Description);
        Assert.True(editor.IsInvalid);
        Assert.False(editor.IsEnabled);
        Assert.Contains(SemanticProperty.IsInvalid, changes);
        Assert.Contains(SemanticProperty.Description, changes);
        Assert.Equal("Identity", Assert.Single(semantics.Descendants(), x => x.Element?.Name == "GroupBox").Name);
        Assert.Equal("Already taken", Assert.Single(semantics.Descendants(), x => x.Element?.Name == "FieldError").Name);
        await host.ClickAsync(root.Single("CheckBox"));
        Assert.Equal(0, fixture.CheckChanges);
    }

    [Fact]
    public async Task Forms_scope_enter_and_reset_without_mutating_the_model()
    {
        var root = host.Render<FormFixture>();
        var fixture = (FormFixture)root.Component;
        scene.Update();
        input.Focus(root.Elements("TextFieldInput").Single(x => x.Id == "second-input"), visible: true);
        Assert.True(input.KeyDown("Enter", KeyModifiers.None, false));
        Assert.Equal(0, fixture.FirstSubmits);
        Assert.Equal(1, fixture.SecondSubmits);
        await host.ClickAsync(root.Elements("Button").Single(x => x.Id == "reset"));
        Assert.Equal(1, fixture.Resets);
        Assert.Equal("kept", root.Elements("TextFieldInput").Single(x => x.Id == "second-input").Attributes["value"]);
    }

    [Fact]
    public async Task Readonly_inputs_reject_editing_and_mixed_checkboxes_resolve_to_checked()
    {
        var root = host.Render<InputFixture>();
        scene.Update();
        var editor = root.Single("TextFieldInput");
        input.Focus(editor, visible: true);
        input.TextInput("changed");
        input.CompositionUpdate("composed", 3);
        input.CompositionCommit("composed");
        Assert.False(input.SetText(editor, "changed"));
        Assert.Equal("kept", editor.Attributes["value"]);
        semantics.Update();
        var check = Assert.Single(semantics.Descendants(), x => x.Role == SemanticRole.CheckBox);
        Assert.Null(check.IsChecked);
        Assert.Equal(1d, scene.GetStyle(root.Single("CheckBoxMixed")).Get(Nazor.Style.StyleProperties.StrokeTrimEnd));
        Assert.Equal(0d, scene.GetStyle(root.Single("CheckBoxCheck")).Get(Nazor.Style.StyleProperties.StrokeTrimEnd));
        await host.ClickAsync(root.Single("CheckBox"));
        semantics.Update();
        Assert.True(check.IsChecked);
        Assert.Equal(1d, scene.GetStyle(root.Single("CheckBoxCheck")).Get(Nazor.Style.StyleProperties.StrokeTrimEnd));
        Assert.Equal(0d, scene.GetStyle(root.Single("CheckBoxMixed")).Get(Nazor.Style.StyleProperties.StrokeTrimEnd));
        Assert.True(((InputFixture)root.Component).MixedCleared);
    }

    [Fact]
    public void Tables_share_columns_resolve_spans_and_preserve_interactive_cell_content()
    {
        var root = host.Render<TableFixture>();
        semantics.Update();
        var table = Assert.Single(semantics.Descendants(), x => x.Role == SemanticRole.Table);
        Assert.Equal("Inventory", table.Name);
        Assert.Equal(3, table.RowCount);
        Assert.Equal(3, table.ColumnCount);
        var first = semantics.TableCellAt(table, 1, 0)!;
        Assert.Equal(2, first.RowSpan);
        Assert.Same(first, semantics.TableCellAt(table, 2, 0));
        var merged = semantics.TableCellAt(table, 2, 1)!;
        Assert.Same(merged, semantics.TableCellAt(table, 2, 2));
        Assert.Equal(2, merged.ColumnSpan);
        Assert.Equal(new[] { "Count", "Action" }, semantics.TableHeaders(merged, true).Select(x => x.Name).ToArray());
        Assert.Equal("Fruit", Assert.Single(semantics.TableHeaders(merged, false)).Name);
        Assert.Null(semantics.TableCellAt(table, -1, 0));
        Assert.Null(semantics.TableCellAt(table, 100, 100));
        var header = semantics.TableCellAt(table, 0, 1)!;
        var count = semantics.TableCellAt(table, 1, 1)!;
        Assert.Equal(header.Bounds.X, count.Bounds.X);
        Assert.Equal(header.Bounds.Width, count.Bounds.Width);
        Assert.Equal(header.Bounds.X, merged.Bounds.X);
        Assert.True(merged.Bounds.Width > count.Bounds.Width);
        Assert.Equal(first.Bounds.Y, count.Bounds.Y);
        Assert.True(first.Bounds.Height > count.Bounds.Height);
        var button = Assert.Single(semantics.Descendants(), x => x.Role == SemanticRole.Button);
        Assert.Same(semantics.TableCellAt(table, 1, 2), button.Parent);
        input.Focus(button.Element!, visible: true);
        input.KeyDown("Enter", KeyModifiers.None, false);
        Assert.Equal(1, ((TableFixture)root.Component).Clicks);
        Assert.All(root.Elements("TableRow"), row => Assert.False(row.Attributes.ContainsKey("focusable")));
    }

    [Fact]
    public void Tables_recompute_columns_and_counts_after_structural_changes()
    {
        var root = host.Render<TableFixture>();
        semantics.Update();
        var table = Assert.Single(semantics.Descendants(), x => x.Role == SemanticRole.Table);
        var fixture = (TableFixture)root.Component;
        fixture.Change();
        semantics.Update();
        Assert.Equal(4, table.RowCount);
        Assert.Equal(4, table.ColumnCount);
        Assert.Equal("Extra", semantics.TableCellAt(table, 3, 3)!.Name);
        Assert.Same(semantics.TableCellAt(table, 3, 0), semantics.TableCellAt(table, 3, 2));
        Assert.Equal(1, semantics.TableCellAt(table, 3, 0)!.RowSpan);
    }

    [Fact]
    public async Task Segments_support_keyboard_selection_and_toggle_buttons_keep_pressed_state()
    {
        var root = host.Render<ToggleFixture>();
        var fixture = (ToggleFixture)root.Component;
        semantics.Update();
        var segments = root.Elements("Segment").ToArray();
        Assert.Equal(new[] { -1, 0, -1 }, segments.Select(node => (int)node.Attributes["tabindex"]!));
        input.Focus(segments[1], visible: true);
        Assert.True(input.KeyDown("ArrowLeft", KeyModifiers.None, false));
        Assert.Same(segments[0], input.Focused);
        Assert.Equal("one", fixture.Selected);
        await host.ClickAsync(root.Single("ToggleButton"));
        semantics.Update();
        Assert.True(semantics.Descendants().Single(node => node.AutomationId == "standalone").IsChecked);
        Assert.True(fixture.Standalone);
    }

    [Fact]
    public async Task Accordion_reuses_disclosures_with_group_expansion_and_header_relationships()
    {
        var root = host.Render<AccordionFixture>();
        var fixture = (AccordionFixture)root.Component;
        semantics.Update();
        var headers = root.Elements("AccordionItemHeader").ToArray();
        Assert.Empty(root.Elements("AccordionItemPanelContent"));
        await host.ClickAsync(headers[0]);
        semantics.Update();
        Assert.Equal("First body",root.Single("AccordionItemPanelContent").Text());
        var content = semantics.Descendants().Single(node=>node.Element?.Id == (string?)headers[0].Attributes["controls"]);
        Assert.Equal("First",content.Name);
        Assert.Same(headers[0],content.LabeledBy!.Element);
        Assert.Equal(content.Element!.Id,headers[0].Attributes["controls"]);
        await host.ClickAsync(headers[1]);
        Assert.Equal("Second body",root.Single("AccordionItemPanelContent").Text());
        input.Focus(headers[1],visible:true);
        Assert.True(input.KeyDown("ArrowDown",KeyModifiers.None,false));
        Assert.Same(headers[0],input.Focused);
        Assert.True(input.KeyDown("Home",KeyModifiers.None,false));
        Assert.Equal("Second body",root.Single("AccordionItemPanelContent").Text());
        fixture.Configure(multiple:true,required:true);
        await host.ClickAsync(headers[0]);
        Assert.Equal(2,root.Elements("AccordionItemPanelContent").Count());
        await host.ClickAsync(headers[1]); await host.ClickAsync(headers[0]);
        Assert.Equal("First body",root.Single("AccordionItemPanelContent").Text());
        Assert.Equal(4,fixture.Changes);
    }

    [Fact]
    public void Progress_shapes_share_range_semantics_and_meter_exposes_threshold_quality()
    {
        var root = host.Render<RangeFixture>();
        semantics.Update();
        var progresses = semantics.Descendants().Where(node=>node.Role==SemanticRole.ProgressBar).ToArray();
        Assert.Equal(3,progresses.Length);
        Assert.All(progresses.Take(2),node=>Assert.Equal(75,node.Range!.Value));
        Assert.Null(progresses[2].Range);
        var meter = Assert.Single(semantics.Descendants(),node=>node.Role==SemanticRole.Meter);
        Assert.Equal(0.9,meter.Range!.Value); Assert.True(meter.Range.IsReadOnly);
        Assert.Equal("worst",root.Single("Meter").Attributes["quality"]);
        Assert.False(semantics.SetRangeValue(meter,0.5));
        Assert.Single(root.Elements("ProgressBar"));
        Assert.Equal(2, root.Elements("Spinner").Count());
    }

    internal sealed class ResizeFixture : ComponentBase
    {
        private double width = 200;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ResizeHandle>(0);
            builder.AddComponentParameter(1, nameof(ResizeHandle.Width), width);
            builder.AddComponentParameter(2, nameof(ResizeHandle.MaxSize), 400d);
            builder.AddComponentParameter(3, nameof(ResizeHandle.AccessibleName), "Pane width");
            builder.AddComponentParameter(4, nameof(ResizeHandle.OnResize), EventCallback.Factory.Create<ResizeValue>(this, size => width = size.Width));
            builder.CloseComponent();
        }
    }

    private sealed class ToggleFixture : ComponentBase
    {
        public string Selected { get; private set; } = "two";
        public bool Standalone { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<SegmentedControl<string>>(0);
            b.AddComponentParameter(1, "Value", Selected);
            b.AddComponentParameter(2, "ValueChanged", EventCallback.Factory.Create<string>(this, value => Selected = value));
            b.AddComponentParameter(3, "ChildContent", (RenderFragment)(group =>
            {
                foreach (var value in new[] { "one", "two", "disabled" })
                {
                    group.OpenComponent<Segment<string>>(0);
                    group.SetKey(value);
                    group.AddComponentParameter(1, "Value", value);
                    group.AddComponentParameter(2, "Disabled", value == "disabled");
                    group.AddComponentParameter(3, "ChildContent", Text(value));
                    group.CloseComponent();
                }
            }));
            b.CloseComponent();
            b.OpenComponent<ToggleButton>(10);
            b.AddComponentParameter(11, "Id", "standalone");
            b.AddComponentParameter(12, "IsPressedChanged", EventCallback.Factory.Create<bool>(this, value => Standalone = value));
            b.CloseComponent();
        }
    }

    private sealed class AccordionFixture : ComponentBase
    {
        private bool multiple,required;
        public int Changes;
        public void Configure(bool multiple,bool required) { this.multiple=multiple; this.required=required; StateHasChanged(); }
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Accordion>(0); b.AddComponentParameter(1,"ExpandMode",multiple ? AccordionExpandMode.Multiple : AccordionExpandMode.Single); b.AddComponentParameter(2,"RequireOne",required);
            b.AddComponentParameter(3,"ExpandedValuesChanged",EventCallback.Factory.Create<IReadOnlyCollection<string>>(this,_=>Changes++));
            b.AddComponentParameter(4,"ChildContent",(RenderFragment)(group=>
            {
                foreach (var name in new[] { "First","Second","Disabled" })
                {
                    group.OpenComponent<AccordionItem>(0); group.SetKey(name); group.AddComponentParameter(1,"Value",name);
                    group.AddComponentParameter(2,"Header",name); group.AddComponentParameter(3,"Disabled",name=="Disabled");
                    group.AddComponentParameter(4,"ChildContent",Text(name+" body")); group.CloseComponent();
                }
            })); b.CloseComponent();
        }
    }

    private sealed class RangeFixture : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<ProgressBar>(0); b.AddComponentParameter(1,"Value",75d); b.CloseComponent();
            b.OpenComponent<Spinner>(4); b.AddComponentParameter(5,"Value",75d); b.CloseComponent();
            b.OpenComponent<Spinner>(10); b.CloseComponent();
            b.OpenComponent<Meter>(20); b.AddComponentParameter(21,"Value",0.9); b.AddComponentParameter(22,"Low",0.2); b.AddComponentParameter(23,"High",0.7); b.AddComponentParameter(24,"Optimum",0.1); b.CloseComponent();
        }
    }

    internal sealed class TableFixture : ComponentBase
    {
        public int Clicks;
        private bool extra;
        public void Change() { extra = true; StateHasChanged(); }
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Table>(0); b.AddComponentParameter(1, "Caption", Text("Inventory"));
            b.AddComponentParameter(2, "ChildContent", (RenderFragment)(table =>
            {
                table.OpenComponent<TableHeader>(0); table.AddComponentParameter(1, "ChildContent", (RenderFragment)(header =>
                {
                    Row(header, 0, cells => { Header(cells, 0, "Category"); Header(cells, 10, "Count"); Header(cells, 20, "Action"); });
                })); table.CloseComponent();
                table.OpenComponent<TableBody>(2); table.AddComponentParameter(3, "ChildContent", (RenderFragment)(body =>
                {
                    Row(body, 0, cells =>
                    {
                        cells.OpenComponent<TableHeaderCell>(0); cells.AddComponentParameter(1, "Scope", TableHeaderScope.Row);
                        cells.AddComponentParameter(2, "RowSpan", 2); cells.AddComponentParameter(3, "ChildContent", Text("Fruit")); cells.CloseComponent();
                        Cell(cells, 10, 1, 1, Text("Two hundred"));
                        Cell(cells, 20, 1, 1, button => { button.OpenComponent<Button>(0); button.AddComponentParameter(1, "OnClick", EventCallback.Factory.Create<PointerEventArgs>(this, _ => Clicks++)); button.AddComponentParameter(2, "ChildContent", Text("Edit")); button.CloseComponent(); });
                    });
                    Row(body, 10, cells => Cell(cells, 0, 2, 1, Text("Combined detail")));
                    if (extra) Row(body, 20, cells => { Cell(cells, 0, 3, 8, Text("Spanning")); Cell(cells, 10, 1, 1, Text("Extra")); });
                })); table.CloseComponent();
            })); b.CloseComponent();
        }
        private static void Row(RenderTreeBuilder b, int sequence, RenderFragment content)
        {
            b.OpenComponent<TableRow>(sequence); b.AddComponentParameter(sequence + 1, "ChildContent", content); b.CloseComponent();
        }
        private static void Header(RenderTreeBuilder b, int sequence, string text)
        {
            b.OpenComponent<TableHeaderCell>(sequence); b.AddComponentParameter(sequence + 1, "ChildContent", Text(text)); b.CloseComponent();
        }
        private static void Cell(RenderTreeBuilder b, int sequence, int columns, int rows, RenderFragment content)
        {
            b.OpenComponent<TableCell>(sequence); b.AddComponentParameter(sequence + 1, "ColumnSpan", columns);
            b.AddComponentParameter(sequence + 2, "RowSpan", rows); b.AddComponentParameter(sequence + 3, "ChildContent", content); b.CloseComponent();
        }
    }

    private sealed class ListFixture : ComponentBase
    {
        private bool reversed;
        public void Reverse() { reversed = true; StateHasChanged(); }
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Controls.List>(0); b.AddComponentParameter(1, "Ordered", true);
            b.AddComponentParameter(2, "Start", 3); b.AddComponentParameter(3, "Reversed", reversed);
            b.AddComponentParameter(4, "ChildContent", (RenderFragment)(items =>
            {
                Item(items, 0, null, "First");
                items.OpenComponent<ListItem>(10); items.AddComponentParameter(11, "Value", 8);
                items.AddComponentParameter(12, "ChildContent", (RenderFragment)(nested =>
                {
                    nested.AddContent(0, "Second"); nested.OpenComponent<Controls.List>(1);
                    nested.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner => Item(inner, 0, null, "Nested"))); nested.CloseComponent();
                })); items.CloseComponent(); Item(items, 20, null, "Third");
            })); b.CloseComponent();
        }
        private static void Item(RenderTreeBuilder b, int sequence, int? value, string text)
        {
            b.OpenComponent<ListItem>(sequence); b.AddComponentParameter(sequence + 1, "Value", value);
            b.AddComponentParameter(sequence + 2, "ChildContent", (RenderFragment)(body => body.AddContent(0, text))); b.CloseComponent();
        }
    }

    private sealed class FieldFixture : ComponentBase
    {
        private bool invalid, disabled;
        public int CheckChanges;
        public void Change(bool invalid, bool disabled) { this.invalid = invalid; this.disabled = disabled; StateHasChanged(); }
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<GroupBox>(0); b.AddComponentParameter(1, "Disabled", disabled);
            b.AddComponentParameter(20, "Header", "Identity");
            b.AddComponentParameter(2, "ChildContent", (RenderFragment)(group =>
            {
                group.OpenComponent<Field>(2); group.AddComponentParameter(3, "Label", Text("Account"));
                group.AddComponentParameter(4, "Description", Text("Use your name")); group.AddComponentParameter(5, "Error", Text("Already taken"));
                group.AddComponentParameter(6, "Required", true); group.AddComponentParameter(7, "Invalid", invalid);
                group.AddComponentParameter(8, "ChildContent", (RenderFragment)(field => { field.OpenComponent<Controls.TextField>(0); field.CloseComponent(); })); group.CloseComponent();
                group.OpenComponent<CheckBox>(9); group.AddComponentParameter(10, "IsCheckedChanged", EventCallback.Factory.Create<bool>(this, _ => CheckChanges++)); group.CloseComponent();
            })); b.CloseComponent();
        }
    }

    private sealed class FormFixture : ComponentBase
    {
        public int FirstSubmits, SecondSubmits, Resets;
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Controls.Form>(0); b.AddComponentParameter(1, "OnSubmit", EventCallback.Factory.Create(this, () => FirstSubmits++));
            b.AddComponentParameter(2, "ChildContent", (RenderFragment)(form =>
            {
                form.OpenComponent<Controls.TextField>(0); form.AddComponentParameter(1, "Id", "first"); form.CloseComponent();
                form.OpenComponent<Button>(2); form.AddComponentParameter(3, "Type", ButtonType.Submit); form.CloseComponent();
            })); b.CloseComponent();
            b.OpenComponent<Controls.Form>(10); b.AddComponentParameter(11, "OnSubmit", EventCallback.Factory.Create(this, () => SecondSubmits++));
            b.AddComponentParameter(12, "OnReset", EventCallback.Factory.Create(this, () => Resets++));
            b.AddComponentParameter(13, "ChildContent", (RenderFragment)(form =>
            {
                form.OpenComponent<Controls.TextField>(0); form.AddComponentParameter(1, "Id", "second"); form.AddComponentParameter(2, "Value", "kept"); form.CloseComponent();
                form.OpenComponent<Button>(3); form.AddComponentParameter(4, "Id", "reset"); form.AddComponentParameter(5, "Type", ButtonType.Reset); form.CloseComponent();
            })); b.CloseComponent();
        }
    }

    private sealed class InputFixture : ComponentBase
    {
        public bool MixedCleared;
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Controls.TextField>(0); b.AddComponentParameter(1, "ReadOnly", true); b.AddComponentParameter(2, "Value", "kept"); b.CloseComponent();
            b.OpenComponent<CheckBox>(3); b.AddComponentParameter(4, "IsIndeterminate", true);
            b.AddComponentParameter(5, "IsIndeterminateChanged", EventCallback.Factory.Create<bool>(this, mixed => MixedCleared = !mixed)); b.CloseComponent();
        }
    }
    private static RenderFragment Text(string text) => b => b.AddContent(0, text);
}
