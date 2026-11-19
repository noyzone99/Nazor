using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Platform.Windows;
using Nazor.Platform.Windows.Automation;
using Nazor.Samples.Counter;
using Nazor.Testing;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class UiAutomationTests
{
    private static readonly ApplicationRuntimeSettings Options = new() { Width = 360, Height = 300 };

    private static WindowHarness Open(Type root)
    {
        var harness = new WindowHarness(root, Options);
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        return harness;
    }

    [Fact]
    public void Standalone_splitter_exposes_its_adjustable_range_through_the_native_provider()
    {
        using var harness = Open(typeof(SemanticControlsTests.ResizeFixture));
        harness.Read(window=>
        {
            window.Semantics.Update();
            using var bridge = new AutomationBridge((Win32Window)window.PlatformWindow,window.Semantics);
            var node = Assert.Single(window.Semantics.Descendants(),node=>node.Role==Accessibility.SemanticRole.Splitter);
            var range = Assert.IsAssignableFrom<IRangeValueProvider>(bridge.Wrap(node).GetPatternProvider(Uia.RangeValuePattern));
            Assert.Equal(200,range.GetValue()); Assert.False(range.GetIsReadOnly());
            range.SetValue(250); window.Semantics.Update(); Assert.Equal(250,range.GetValue());
            Assert.Throws<ArgumentOutOfRangeException>(()=>range.SetValue(-1)); return true;
        });
    }

    [Fact]
    public void Table_provider_exposes_grid_coordinates_spans_and_headers()
    {
        using var harness = Open(typeof(SemanticControlsTests.TableFixture));
        harness.Read(window =>
        {
            var semantics = window.Semantics;
            semantics.Update();
            using var bridge = new AutomationBridge((Win32Window)window.PlatformWindow, semantics);
            var table = Assert.Single(semantics.Descendants(), node => node.Role == Accessibility.SemanticRole.Table);
            var provider = bridge.Wrap(table);
            var grid = Assert.IsAssignableFrom<IGridProvider>(provider.GetPatternProvider(10006));
            Assert.Equal(3, grid.GetRowCount()); Assert.Equal(3, grid.GetColumnCount());
            var cell = Assert.IsAssignableFrom<IGridItemProvider>(grid.GetItem(2, 0));
            Assert.Equal(1, cell.GetRow()); Assert.Equal(0, cell.GetColumn()); Assert.Equal(2, cell.GetRowSpan());
            Assert.Same(provider, cell.GetContainingGrid());
            var headers = Assert.IsAssignableFrom<ITableItemProvider>(grid.GetItem(2, 1));
            var array = headers.GetColumnHeaderItems();
            Assert.NotEqual(0, array);
            UiaClient.DestroySafeArray(array);
            Assert.Null(provider.GetPatternProvider(Uia.SelectionPattern));
            return true;
        });
    }

    [Fact]
    public void Window_local_dialog_exposes_window_pattern_and_ordinary_close_behavior()
    {
        using var harness = Open(typeof(OverlayCompositionTests.DialogFixture));
        harness.Read(window=>
        {
            var trigger = window.Semantics.Descendants().Single(node=>node.AutomationId=="open");
            window.Input.Focus(trigger.Element!,visible:true); window.Input.Activate(trigger.Element!);
            return true;
        });
        harness.WaitUntil(window=>window.Semantics.Descendants().Any(node=>node.Role==Accessibility.SemanticRole.Dialog),"dialog opened");
        harness.Read(window=>
        {
            using var bridge = new AutomationBridge((Win32Window)window.PlatformWindow,window.Semantics);
            var dialog = window.Semantics.Descendants().Single(node=>node.Role==Accessibility.SemanticRole.Dialog);
            var provider = bridge.Wrap(dialog);
            Assert.Equal(true,provider.GetPropertyValue(30174).ToObject());
            var pattern = Assert.IsAssignableFrom<IWindowProvider>(provider.GetPatternProvider(10009));
            Assert.True(pattern.GetIsModal()); Assert.False(pattern.GetCanMaximize()); Assert.False(pattern.GetCanMinimize());
            Assert.Throws<InvalidOperationException>(()=>pattern.SetVisualState(1));
            pattern.Close(); return true;
        });
        harness.WaitUntil(window=>!window.Semantics.Descendants().Any(node=>node.Role==Accessibility.SemanticRole.Dialog),"dialog closed");
    }

    [Theory]
    [InlineData(Accessibility.SemanticRole.RadioButton)]
    [InlineData(Accessibility.SemanticRole.ListItem)]
    [InlineData(Accessibility.SemanticRole.TreeItem)]
    [InlineData(Accessibility.SemanticRole.TabItem)]
    public void Items_that_are_selected_announce_SelectionItem_not_toggle_state(Accessibility.SemanticRole role)
    {
        Assert.Equal(Uia.SelectionItemIsSelectedProperty, AutomationBridge.PropertyOf(role, Accessibility.SemanticProperty.IsChecked));
        Assert.Equal(Uia.ToggleStateProperty, AutomationBridge.PropertyOf(Accessibility.SemanticRole.CheckBox, Accessibility.SemanticProperty.IsChecked));
    }

    [Fact]
    public void Tree_exposes_names_roles_and_ids_under_the_window()
    {
        using var harness = Open(typeof(Settings));
        var (window, children, rows, framework) = UiaClient.Run(uia =>
        {
            var root = uia.ElementFromHandle(harness.Handle);
            var children = uia.FindAll(root, TreeScope.Children)
                .Select(e => (Type: e.GetCurrentControlType(), Name: e.GetCurrentName(), Id: e.GetCurrentAutomationId()))
                .ToList();
            var all = uia.FindAll(root, TreeScope.Descendants);
            var rows = all.Count(e => e.GetCurrentAutomationId().StartsWith("row", StringComparison.Ordinal) && e.GetCurrentControlType() == Uia.TextControlType);
            var framework = all.Single(e => e.GetCurrentAutomationId() == "save").GetCurrentFrameworkId();
            return ((root.GetCurrentControlType(), root.GetCurrentName()), children, rows, framework);
        });

        Assert.Equal((Uia.WindowControlType, "Impostazioni"), window);
        Assert.Contains(children, c => c.Type == UiaClient.TitleBarControlType);
        Assert.Equal(
            [
                (Uia.CheckBoxControlType, "Attivo", "active"),
                (Uia.ButtonControlType, "Salva le impostazioni", "save"),
                (Uia.GroupControlType, "Riepilogo", ""),
                (Uia.PaneControlType, "", "list"),
            ],
            children.Where(c => c.Type != UiaClient.TitleBarControlType));
        Assert.Equal(30, rows);
        Assert.Equal("Nazor", framework);
        Assert.True(harness.Read(w => w.HasSemantics));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Invoke_and_toggle_patterns_drive_the_controls()
    {
        using var harness = Open(typeof(Settings));
        var (stateAfterToggle, buttonHasToggle) = UiaClient.Run(uia =>
        {
            var check = uia.Find(harness.Handle, "active").Pattern<IUIAutomationTogglePattern>(Uia.TogglePattern);
            check.Toggle();
            var save = uia.Find(harness.Handle, "save");
            save.Pattern<IUIAutomationInvokePattern>(Uia.InvokePattern).Invoke();
            return (check.GetCurrentToggleState(), save.GetCurrentPattern(Uia.TogglePattern) is not null);
        });

        Assert.Equal((int)ToggleState.On, stateAfterToggle);
        Assert.False(buttonHasToggle);
        Assert.True(harness.Read(w => ((Settings)w.Root.Component).Active));
        harness.WaitUntil(w => ((Settings)w.Root.Component).Saves == 1, "the invoked handler");
        harness.WaitUntil(w => w.Scene.GetEffectiveValue(w.Root.Single("CheckBoxCheck"), Nazor.Style.StyleProperties.StrokeTrimEnd) >= 0.999, "the visible check stroke");
    }

    [Fact]
    public void Selection_item_toggle_and_range_value_patterns_drive_the_choices()
    {
        using var harness = Open(typeof(Choices));
        var (types, mediumSelected, addError, outOfRange, range) = UiaClient.Run(uia =>
        {
            var types = new[] { "size-S", "notify", "volume" }.Select(id => uia.Find(harness.Handle, id).GetCurrentControlType()).ToArray();
            var small = uia.Find(harness.Handle, "size-S").Pattern<IUIAutomationSelectionItemPattern>(Uia.SelectionItemPattern);
            var medium = uia.Find(harness.Handle, "size-M").Pattern<IUIAutomationSelectionItemPattern>(Uia.SelectionItemPattern);
            var mediumSelected = medium.GetCurrentIsSelected() != 0;
            small.Select();
            int? addError = null;
            try
            {
                uia.Find(harness.Handle, "size-L").Pattern<IUIAutomationSelectionItemPattern>(Uia.SelectionItemPattern).AddToSelection();
            }
            catch (Exception exception)
            {
                addError = exception.HResult;
            }
            uia.Find(harness.Handle, "notify").Pattern<IUIAutomationTogglePattern>(Uia.TogglePattern).Toggle();
            var volume = uia.Find(harness.Handle, "volume").Pattern<IUIAutomationRangeValuePattern>(Uia.RangeValuePattern);
            volume.SetValue(60);
            var outOfRange = false;
            try
            {
                volume.SetValue(500);
            }
            catch (Exception)
            {
                outOfRange = true;
            }
            var download = uia.Find(harness.Handle, "download");
            var progress = download.Pattern<IUIAutomationRangeValuePattern>(Uia.RangeValuePattern);
            var readOnly = progress.GetCurrentIsReadOnly() != 0 && progress.GetCurrentValue() == 30;
            try
            {
                progress.SetValue(40);
                readOnly = false;
            }
            catch (Exception)
            {
            }
            var busy = uia.Find(harness.Handle, "busy");
            types = [.. types, download.GetCurrentControlType(), busy.GetCurrentControlType()];
            outOfRange &= readOnly && busy.GetCurrentPattern(Uia.RangeValuePattern) is null;
            return (types, mediumSelected, addError, outOfRange,
                (volume.GetCurrentMinimum(), volume.GetCurrentMaximum(), volume.GetCurrentSmallChange(), volume.GetCurrentLargeChange(), volume.GetCurrentIsReadOnly() != 0));
        });

        Assert.Equal([Uia.RadioButtonControlType, Uia.ButtonControlType, Uia.SliderControlType, Uia.ProgressBarControlType, Uia.ProgressBarControlType], types);
        Assert.True(mediumSelected);
        Assert.NotNull(addError);
        Assert.True(outOfRange);
        Assert.Equal((0, 100, 10, 20, false), range);
        harness.WaitUntil(w => ((Choices)w.Root.Component) is { Size: "S", Notify: true, Volume: 60 }, "the choices made through UI Automation");
        var (selected, value) = UiaClient.Run(uia => (
            uia.Find(harness.Handle, "size-S").Pattern<IUIAutomationSelectionItemPattern>(Uia.SelectionItemPattern).GetCurrentIsSelected() != 0,
            uia.Find(harness.Handle, "volume").Pattern<IUIAutomationRangeValuePattern>(Uia.RangeValuePattern).GetCurrentValue()));
        Assert.True(selected);
        Assert.Equal(60, value);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void A_combo_box_expands_shows_its_value_and_its_items_can_be_selected()
    {
        using var harness = Open(typeof(CountryPicker));
        var (type, value, readOnly, collapsed) = UiaClient.Run(uia =>
        {
            var combo = uia.Find(harness.Handle, "country");
            var text = combo.Pattern<IUIAutomationValuePattern>(Uia.ValuePattern);
            var expand = combo.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern);
            var collapsed = expand.GetCurrentExpandCollapseState();
            expand.Expand();
            return (combo.GetCurrentControlType(), text.GetCurrentValue(), text.GetCurrentIsReadOnly() != 0, collapsed);
        });
        Assert.Equal(Uia.ComboBoxControlType, type);
        Assert.Equal("Francia", value);
        Assert.True(readOnly);
        Assert.Equal((int)ExpandCollapseState.Collapsed, collapsed);
        harness.WaitUntil(w => w.Scene.Overlays.Any(o => o.Name == "SelectPopup"), "the open list");

        var (expanded, itemType, selected, container) = UiaClient.Run(uia =>
        {
            var combo = uia.Find(harness.Handle, "country");
            var items = uia.FindAll(combo, TreeScope.Descendants).Where(e => e.GetCurrentControlType() == Uia.ListItemControlType).ToList();
            var france = items.Single(e => e.GetCurrentName() == "Francia").Pattern<IUIAutomationSelectionItemPattern>(Uia.SelectionItemPattern);
            var germany = items.Single(e => e.GetCurrentName() == "Germania");
            var selection = germany.Pattern<IUIAutomationSelectionItemPattern>(Uia.SelectionItemPattern);
            var container = selection.GetCurrentSelectionContainer()?.GetCurrentAutomationId();
            var state = combo.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).GetCurrentExpandCollapseState();
            var franceSelected = france.GetCurrentIsSelected() != 0;
            var itemType = germany.GetCurrentControlType();
            selection.Select();
            return (state, itemType, franceSelected, container);
        });
        Assert.Equal((int)ExpandCollapseState.Expanded, expanded);
        Assert.Equal(Uia.ListItemControlType, itemType);
        Assert.True(selected);
        Assert.Equal("country", container);
        harness.WaitUntil(w => ((CountryPicker)w.Root.Component).Country == "DE" && !w.Scene.Overlays.Any(), "the chosen item and the closed list");
        Assert.Equal("Germania", UiaClient.Run(uia => uia.Find(harness.Handle, "country").Pattern<IUIAutomationValuePattern>(Uia.ValuePattern).GetCurrentValue()));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void A_date_picker_is_a_combo_box_with_its_date_that_expands_into_a_calendar()
    {
        using var harness = Open(typeof(Booking));
        var (type, value) = UiaClient.Run(uia =>
        {
            var picker = uia.Find(harness.Handle, "arrival-input");
            var value = picker.Pattern<IUIAutomationValuePattern>(Uia.ValuePattern).GetCurrentValue();
            picker.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).Expand();
            return (picker.GetCurrentControlType(), value);
        });
        Assert.Equal(Uia.ComboBoxControlType, type);
        Assert.Equal("15/10/2026", value);
        harness.WaitUntil(w => w.Scene.Overlays.Any(o => o.Name == "DatePickerPopup"), "the open calendar");
        var (calendars, days) = UiaClient.Run(uia =>
        {
            var all = uia.FindAll(uia.ElementFromHandle(harness.Handle), TreeScope.Descendants);
            return (all.Count(e => e.GetCurrentControlType() == Uia.CalendarControlType), all.Count(e => e.GetCurrentControlType() == Uia.ListItemControlType));
        });
        Assert.Equal(1, calendars);
        Assert.Equal(DateTime.DaysInMonth(2026, 10), days);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Menu_bars_and_tool_bars_have_their_control_types_and_bar_items_expand()
    {
        using var harness = Open(typeof(Editor));
        var (bar, tools, item, state) = UiaClient.Run(uia =>
        {
            var file = uia.Find(harness.Handle, "file");
            var expand = file.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern);
            var state = expand.GetCurrentExpandCollapseState();
            expand.Expand();
            return (uia.Find(harness.Handle, "menus").GetCurrentControlType(), uia.Find(harness.Handle, "tools").GetCurrentControlType(), file.GetCurrentControlType(), state);
        });
        Assert.Equal(Uia.MenuBarControlType, bar);
        Assert.Equal(Uia.ToolBarControlType, tools);
        Assert.Equal(Uia.MenuItemControlType, item);
        Assert.Equal((int)ExpandCollapseState.Collapsed, state);
        harness.WaitUntil(w => w.Scene.Overlays.Any(o => o.Name == "Menu"), "the open menu");
        Assert.Equal((int)ExpandCollapseState.Expanded, UiaClient.Run(uia =>
            uia.Find(harness.Handle, "file").Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).GetCurrentExpandCollapseState()));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void A_split_button_is_invoked_and_expanded_through_its_patterns()
    {
        using var harness = Open(typeof(SaveCommands));
        var (type, sortType, state) = UiaClient.Run(uia =>
        {
            var save = uia.Find(harness.Handle, "save");
            var state = save.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).GetCurrentExpandCollapseState();
            save.Pattern<IUIAutomationInvokePattern>(Uia.InvokePattern).Invoke();
            save.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).Expand();
            return (save.GetCurrentControlType(), uia.Find(harness.Handle, "sort").GetCurrentControlType(), state);
        });
        Assert.Equal(Uia.SplitButtonControlType, type);
        Assert.Equal(Uia.ButtonControlType, sortType);
        Assert.Equal((int)ExpandCollapseState.Collapsed, state);
        harness.WaitUntil(w => w.Scene.Overlays.Any(o => o.Name == "Menu"), "the open menu");
        var (expanded, items) = UiaClient.Run(uia =>
        {
            var save = uia.Find(harness.Handle, "save");
            var items = uia.FindAll(save, TreeScope.Descendants).Where(e => e.GetCurrentControlType() == Uia.MenuItemControlType).Select(e => e.GetCurrentName()).ToList();
            return (save.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).GetCurrentExpandCollapseState(), items);
        });
        Assert.Equal((int)ExpandCollapseState.Expanded, expanded);
        Assert.Equal(["Salva con nome"], items);
        Assert.Equal(["save"], harness.Read(w => ((SaveCommands)w.Root.Component).Log.ToList()));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void A_tree_item_expands_through_its_pattern_and_tells_its_level()
    {
        using var harness = Open(typeof(FolderTree));
        harness.WaitUntil(w => w.Root.Elements().Count(e => e.Name == "TreeViewItem") == 2, "the two roots");
        var (treeType, itemType, leafState, level) = UiaClient.Run(uia =>
        {
            var tree = uia.Find(harness.Handle, "folders");
            var items = uia.FindAll(tree, TreeScope.Descendants);
            var documents = items.Single(e => e.GetCurrentName() == "Documenti");
            var images = items.Single(e => e.GetCurrentName() == "Immagini");
            var leafState = images.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).GetCurrentExpandCollapseState();
            var level = documents.GetCurrentPropertyValue(Uia.LevelProperty).ToObject();
            documents.Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).Expand();
            return (tree.GetCurrentControlType(), documents.GetCurrentControlType(), leafState, level);
        });
        Assert.Equal(Uia.TreeControlType, treeType);
        Assert.Equal(Uia.TreeItemControlType, itemType);
        Assert.Equal((int)ExpandCollapseState.LeafNode, leafState);
        Assert.Equal(1, level);
        harness.WaitUntil(w => w.Root.Elements().Count(e => e.Name == "TreeViewItem") == 4, "the expanded folder");
        var (state, childLevel) = UiaClient.Run(uia =>
        {
            var items = uia.FindAll(uia.Find(harness.Handle, "folders"), TreeScope.Descendants);
            return (items.Single(e => e.GetCurrentName() == "Documenti").Pattern<IUIAutomationExpandCollapsePattern>(Uia.ExpandCollapsePattern).GetCurrentExpandCollapseState(),
                items.Single(e => e.GetCurrentName() == "Fatture").GetCurrentPropertyValue(Uia.LevelProperty).ToObject());
        });
        Assert.Equal((int)ExpandCollapseState.Expanded, state);
        Assert.Equal(2, childLevel);
        Assert.Null(harness.Read(w => ((FolderTree)w.Root.Component).Selected));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Labels_help_texts_descriptions_positions_and_role_descriptions_reach_ui_automation()
    {
        using (var harness = Open(typeof(Described)))
        {
            var (name, label, description, help, refresh, saveHelp) = UiaClient.Run(uia =>
            {
                var sensor = uia.Find(harness.Handle, "sensor-input");
                var labeledBy = sensor.GetCurrentPropertyValue(Uia.LabeledByProperty).ToObject() as IUIAutomationElement;
                return (sensor.GetCurrentName(), labeledBy?.GetCurrentAutomationId(), sensor.GetCurrentPropertyValue(Uia.FullDescriptionProperty).ToObject(),
                    sensor.GetCurrentPropertyValue(Uia.HelpTextProperty).ToObject(), uia.Find(harness.Handle, "refresh").GetCurrentName(),
                    uia.Find(harness.Handle, "save").GetCurrentPropertyValue(Uia.HelpTextProperty).ToObject());
            });
            Assert.Equal(("Nome del sensore", "sensor-label"), (name, label));
            Assert.Equal(("Il nome che compare nei report", "Il nome che compare nei report"), (description, help));
            Assert.Equal(("Aggiorna", "Salva le modifiche (Ctrl+S)"), (refresh, saveHelp));
        }

        using var choices = Open(typeof(Choices));
        var (position, size, switchType, sliderType) = UiaClient.Run(uia =>
        {
            var medium = uia.Find(choices.Handle, "size-M");
            return (medium.GetCurrentPropertyValue(Uia.PositionInSetProperty).ToObject(), medium.GetCurrentPropertyValue(Uia.SizeOfSetProperty).ToObject(),
                uia.Find(choices.Handle, "notify").GetCurrentPropertyValue(Uia.LocalizedControlTypeProperty).ToObject(),
                uia.Find(choices.Handle, "volume").GetCurrentPropertyValue(Uia.LocalizedControlTypeProperty).ToObject());
        });
        Assert.Equal((2, 3), (position, size));
        Assert.Equal(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "it" ? "interruttore" : "switch", switchType);
        Assert.IsType<string>(sliderType);
        Assert.NotEqual(switchType, sliderType);
        Assert.Empty(choices.Read(w => w.Exceptions));
    }

    [Fact]
    public void Live_regions_and_announcements_reach_a_client_that_listens()
    {
        using var harness = Open(typeof(LiveNotices));
        var (live, liveSetting, notification) = UiaClient.Run(uia =>
        {
            var window = uia.ElementFromHandle(harness.Handle);
            var log = new UiaEventLog();
            uia.AddAutomationEventHandler(Uia.LiveRegionChangedEvent, window, TreeScope.Subtree, 0, log);
            ((IUIAutomation5)uia).AddNotificationEventHandler(window, TreeScope.Subtree, 0, log);
            try
            {
                var setting = uia.Find(harness.Handle, "status").GetCurrentPropertyValue(Uia.LiveSettingProperty).ToObject();
                harness.Read(w =>
                {
                    var page = (LiveNotices)w.Root.Component;
                    page.Status = "Salvato";
                    page.Refresh();
                    return true;
                });
                var region = log.Next(e => e.StartsWith("20024 status", StringComparison.Ordinal));
                harness.Read(w =>
                {
                    w.Announce("Tre risultati");
                    return true;
                });
                return (region, setting, log.Next(e => e.StartsWith("notification", StringComparison.Ordinal)));
            }
            finally
            {
                uia.RemoveAllEventHandlers();
            }
        }, automation8: true);
        Assert.Equal("20024 status: Salvato", live);
        Assert.Equal(1, liveSetting);
        Assert.Equal("notification Tre risultati (2) Nazor.Announcement", notification);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Lists_and_tab_lists_tell_their_selection()
    {
        using (var harness = Open(typeof(Recipients)))
        {
            harness.WaitUntil(w => w.Root.Elements().Count(e => e.Name == "ListViewItem") == 4, "the rows");
            var (names, multiple, required, events) = UiaClient.Run(uia =>
            {
                var list = uia.Find(harness.Handle, "recipients");
                var log = new UiaEventLog();
                uia.AddAutomationEventHandler(Uia.SelectionItemElementAddedToSelectionEvent, list, TreeScope.Subtree, 0, log);
                try
                {
                    var selection = list.Pattern<IUIAutomationSelectionPattern>(Uia.SelectionPattern);
                    var current = selection.GetCurrentSelection();
                    var names = Enumerable.Range(0, current.GetLength()).Select(i => current.GetElement(i).GetCurrentName()).ToList();
                    var bruno = uia.FindAll(list, TreeScope.Descendants).Single(e => e.GetCurrentName() == "Bruno");
                    bruno.Pattern<IUIAutomationSelectionItemPattern>(Uia.SelectionItemPattern).AddToSelection();
                    return (names, selection.GetCurrentCanSelectMultiple(), selection.GetCurrentIsSelectionRequired(), log.Next(e => e.StartsWith("20010", StringComparison.Ordinal)));
                }
                finally
                {
                    uia.RemoveAllEventHandlers();
                }
            });
            Assert.Equal(["Anna", "Carla"], names);
            Assert.Equal((1, 0), (multiple, required));
            Assert.StartsWith("20010", events, StringComparison.Ordinal);
            Assert.Equal(["Anna", "Bruno", "Carla"], harness.Read(w => ((Recipients)w.Root.Component).Chosen.Order().ToList()));
            Assert.Empty(harness.Read(w => w.Exceptions));
        }

        using var pages = Open(typeof(Pages));
        var (tabs, tabRequired) = UiaClient.Run(uia =>
        {
            var tabList = uia.FindAll(uia.ElementFromHandle(pages.Handle), TreeScope.Descendants).Single(e => e.GetCurrentControlType() == Uia.TabControlType);
            var selection = tabList.Pattern<IUIAutomationSelectionPattern>(Uia.SelectionPattern);
            var current = selection.GetCurrentSelection();
            return (Enumerable.Range(0, current.GetLength()).Select(i => current.GetElement(i).GetCurrentName()).ToList(), selection.GetCurrentIsSelectionRequired());
        });
        Assert.Equal(["Uno"], tabs);
        Assert.Equal(1, tabRequired);
        Assert.Empty(pages.Read(w => w.Exceptions));
    }

    [Fact]
    public void The_text_pattern_reads_moves_and_selects_the_text_of_a_box()
    {
        using var harness = Open(typeof(Form));
        var result = UiaClient.Run(uia =>
        {
            var box = uia.Find(harness.Handle, "name-input");
            var log = new UiaEventLog();
            uia.AddAutomationEventHandler(Uia.TextSelectionChangedEvent, box, TreeScope.Element, 0, log);
            try
            {
                var text = box.Pattern<IUIAutomationTextPattern2>(Uia.TextPattern2);
                var document = text.GetDocumentRange();
                var whole = document.GetText(-1);
                var range = document.Clone();
                range.ExpandToEnclosingUnit((int)Accessibility.TextUnit.Character);
                var first = range.GetText(-1);
                var moved = range.Move((int)Accessibility.TextUnit.Character, 1);
                var second = range.GetText(-1);
                range.MoveEndpointByUnit(1, (int)Accessibility.TextUnit.Character, 2);
                var three = range.GetText(-1);
                range.Select();
                var changed = log.Next(e => e.StartsWith("20014", StringComparison.Ordinal));
                var selection = text.GetSelection();
                var selected = selection.GetElement(0).GetText(-1);
                var caret = text.GetCaretRange(out _);
                var afterCaret = document.Clone();
                afterCaret.MoveEndpointByRange(0, caret, 0);
                var word = document.Clone();
                word.ExpandToEnclosingUnit((int)Accessibility.TextUnit.Word);
                var found = document.FindText("SOR", 1, 1)?.GetText(-1);
                var readOnly = document.GetAttributeValue(Uia.IsReadOnlyAttribute).ToObject();
                var enclosing = range.GetEnclosingElement().GetCurrentAutomationId();
                return (whole, first, moved, second, three, changed, selection.GetLength(), selected, afterCaret.GetText(-1), word.GetText(-1), found, readOnly, enclosing, text.GetSupportedTextSelection());
            }
            finally
            {
                uia.RemoveAllEventHandlers();
            }
        }, automation8: true);
        Assert.Equal(("Sensore", "S", 1, "e", "ens"), (result.whole, result.first, result.moved, result.second, result.three));
        Assert.StartsWith("20014 name", result.changed, StringComparison.Ordinal);
        Assert.Equal((1, "ens", "ore"), (result.Item7, result.selected, result.Item9));
        Assert.Equal(("Sensore", "sor", (object?)false, "name-input", 1), (result.Item10, result.found, result.readOnly, result.enclosing, result.Item14));
        Assert.Equal(new Input.TextSelection(1, 4, 4), harness.Read(w => w.Input.GetTextSelection(w.Root.Elements().First(e => e.Attributes.GetValueOrDefault("id") is "name-input"))));

        using var login = Open(typeof(Login));
        Assert.Null(UiaClient.Run(uia => uia.Find(login.Handle, "password-input").GetCurrentPattern(Uia.TextPattern)));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Counter_button_is_invoked_and_the_text_name_follows()
    {
        using var harness = Open(typeof(Counter));
        UiaClient.Run(uia =>
        {
            var button = uia.FindAll(uia.ElementFromHandle(harness.Handle), TreeScope.Descendants)
                .Single(e => e.GetCurrentControlType() == Uia.ButtonControlType && e.GetCurrentFrameworkId() == "Nazor");
            Assert.Equal("Increment", button.GetCurrentName());
            button.Pattern<IUIAutomationInvokePattern>(Uia.InvokePattern).Invoke();
        });
        harness.WaitUntil(w => w.Root.Single("TextBlock").Text() == "Count: 1", "the incremented counter");
        var text = UiaClient.Run(uia => uia.FindAll(uia.ElementFromHandle(harness.Handle), TreeScope.Descendants)
            .Single(e => e.GetCurrentControlType() == Uia.TextControlType && e.GetCurrentFrameworkId() == "Nazor").GetCurrentName());
        Assert.Equal("Count: 1", text);
    }

    [Fact]
    public void Value_pattern_edits_text_boxes_and_disabled_ones_refuse()
    {
        using var harness = Open(typeof(Form));
        const string typed = "Zoë 😀";
        var (before, after, name, lockedEnabled, lockedError) = UiaClient.Run(uia =>
        {
            var box = uia.Find(harness.Handle, "name-input");
            var value = box.Pattern<IUIAutomationValuePattern>(Uia.ValuePattern);
            var before = value.GetCurrentValue();
            value.SetValue(typed);
            var locked = uia.Find(harness.Handle, "locked-input");
            int? error = null;
            try
            {
                locked.Pattern<IUIAutomationValuePattern>(Uia.ValuePattern).SetValue("x");
            }
            catch (Exception exception)
            {
                error = exception.HResult;
            }
            return (before, value.GetCurrentValue(), box.GetCurrentName(), locked.GetCurrentIsEnabled(), error);
        });

        Assert.Equal("Sensore", before);
        Assert.Equal(typed, after);
        Assert.Equal(typed, harness.Read(w => ((Form)w.Root.Component).Name));
        Assert.Equal("", name);
        Assert.Equal(0, lockedEnabled);
        Assert.Equal(Uia.E_ELEMENTNOTENABLED, lockedError);
    }

    [Fact]
    public void Keyboard_focus_bounds_and_hit_testing_match_the_scene()
    {
        using var harness = Open(typeof(Settings));
        harness.Key(0x09, down: true);
        harness.Key(0x09, down: false);
        harness.Key(0x09, down: true);
        harness.Key(0x09, down: false);
        harness.WaitUntil(w => w.Input.Focused?.Id == "save", "focus on the button");

        var (focus, rect) = UiaClient.Run(uia =>
        {
            var save = uia.Find(harness.Handle, "save");
            return (save.GetCurrentHasKeyboardFocus(), save.GetCurrentBoundingRectangle());
        });
        Assert.Equal(1, focus);
        var expected = harness.Read(w =>
        {
            var bounds = w.Scene.GetBounds(w.Root.Elements("Button").Single());
            var origin = new Win32.POINT { X = (int)MathF.Round(bounds.X * w.PlatformWindow.Scale), Y = (int)MathF.Round(bounds.Y * w.PlatformWindow.Scale) };
            Win32.ClientToScreen(harness.Handle, ref origin);
            return (origin.X, origin.Y, (int)MathF.Round(bounds.Width * w.PlatformWindow.Scale));
        });
        Assert.Equal(expected, (rect.Left, rect.Top, rect.Right - rect.Left));

        var hit = harness.Read(w =>
        {
            var bridge = ((Win32Window)w.PlatformWindow).Automation!;
            var row = w.Scene.GetBounds(w.Root.Elements("TextBlock").Single(e => e.Id == "row2"));
            var point = new Win32.POINT { X = (int)((row.X + 4) * w.PlatformWindow.Scale), Y = (int)((row.Y + 4) * w.PlatformWindow.Scale) };
            Win32.ClientToScreen(harness.Handle, ref point);
            return ((AutomationNode)((IRawElementProviderFragmentRoot)bridge.Root).ElementProviderFromPoint(point.X + 0.5, point.Y + 0.5)!).Node.AutomationId;
        });
        Assert.Equal("row2", hit);
    }

    [Fact]
    public void Unnamed_containers_expose_native_scroll_pattern_with_hidden_bars_and_lose_it_when_overflow_is_disabled()
    {
        using var harness = Open(typeof(OverflowPanel));
        var (controlType, horizontal, vertical, x, y, focusable) = UiaClient.Run(uia =>
        {
            var element = uia.Find(harness.Handle, "overflow-panel");
            var scroll = element.Pattern<IUIAutomationScrollPattern>(Uia.ScrollPattern);
            scroll.SetScrollPercent(25, 50);
            return (element.GetCurrentControlType(), scroll.GetCurrentHorizontallyScrollable(), scroll.GetCurrentVerticallyScrollable(),
                scroll.GetCurrentHorizontalScrollPercent(), scroll.GetCurrentVerticalScrollPercent(), element.GetCurrentPropertyValue(Uia.IsKeyboardFocusableProperty).ToObject());
        });
        Assert.Equal(Uia.PaneControlType, controlType);
        Assert.NotEqual(0, horizontal);
        Assert.NotEqual(0, vertical);
        Assert.Equal(25, x, 3);
        Assert.Equal(50, y, 3);
        Assert.Equal(true, focusable);
        var before = harness.Read(w =>
        {
            var page = (OverflowPanel)w.Root.Component;
            page.Named = true;
            page.Overflow = "hidden";
            page.Refresh();
            return w.FrameCount;
        });
        harness.WaitUntil(w => w.FrameCount > before, "overflow policy update");
        var (name, hasScroll, groupType) = UiaClient.Run(uia =>
        {
            var element = uia.Find(harness.Handle, "overflow-panel");
            return (element.GetCurrentName(), element.GetCurrentPattern(Uia.ScrollPattern) is not null, element.GetCurrentControlType());
        });
        Assert.Equal("Scroll test", name);
        Assert.False(hasScroll);
        Assert.Equal(Uia.GroupControlType, groupType);
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Scroll_pattern_moves_the_list_and_updates_offscreen_rows()
    {
        using var harness = Open(typeof(Settings));
        var (scrollable, offscreenBefore, percentAfterPage, offscreenAtEnd, firstAtEnd) = UiaClient.Run(uia =>
        {
            var list = uia.Find(harness.Handle, "list");
            var scroll = list.Pattern<IUIAutomationScrollPattern>(Uia.ScrollPattern);
            var last = uia.Find(harness.Handle, "row30");
            var offscreenBefore = last.GetCurrentIsOffscreen();
            scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            var percent = scroll.GetCurrentVerticalScrollPercent();
            scroll.SetScrollPercent(Uia.NoScroll, 100);
            var first = uia.Find(harness.Handle, "row1");
            return (scroll.GetCurrentVerticallyScrollable(), offscreenBefore, percent, last.GetCurrentIsOffscreen(), first.GetCurrentIsOffscreen());
        });
        Assert.Equal(1, scrollable);
        Assert.Equal(1, offscreenBefore);
        Assert.InRange(percentAfterPage, 1, 99);
        Assert.Equal(0, offscreenAtEnd);
        Assert.Equal(1, firstAtEnd);
    }

    [Fact]
    public void Scroll_pattern_scrolls_sideways_a_horizontal_viewer()
    {
        using var harness = Open(typeof(Gallery));
        var (horizontal, vertical, view, offscreenBefore, percentAfterPage, offscreenAtEnd, verticalRejected) = UiaClient.Run(uia =>
        {
            var strip = uia.Find(harness.Handle, "strip");
            var scroll = strip.Pattern<IUIAutomationScrollPattern>(Uia.ScrollPattern);
            var last = uia.Find(harness.Handle, "tile12");
            var offscreenBefore = last.GetCurrentIsOffscreen();
            scroll.Scroll(ScrollAmount.LargeIncrement, ScrollAmount.NoAmount);
            var percent = scroll.GetCurrentHorizontalScrollPercent();
            scroll.SetScrollPercent(100, Uia.NoScroll);
            var rejected = false;
            try
            {
                scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            return (scroll.GetCurrentHorizontallyScrollable(), scroll.GetCurrentVerticallyScrollable(), scroll.GetCurrentHorizontalViewSize(),
                offscreenBefore, percent, last.GetCurrentIsOffscreen(), rejected);
        });
        Assert.Equal(1, horizontal);
        Assert.Equal(0, vertical);
        Assert.Equal(25, view, 3);
        Assert.Equal(1, offscreenBefore);
        Assert.InRange(percentAfterPage, 1, 99);
        Assert.Equal(0, offscreenAtEnd);
        Assert.True(verticalRejected);
    }

    [Fact]
    public void Pattern_methods_do_not_wait_for_the_window_to_be_focusable()
    {
        using var harness = Open(typeof(Settings));
        var elapsed = UiaClient.Run(uia =>
        {
            var check = uia.Find(harness.Handle, "active").Pattern<IUIAutomationTogglePattern>(Uia.TogglePattern);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < 4; i++) check.Toggle();
            return watch.Elapsed;
        });
        Assert.True(elapsed < TimeSpan.FromSeconds(1), $"Four toggles took {elapsed.TotalMilliseconds:F0} ms.");
        Assert.Equal("active", harness.Read(w => w.Input.Focused?.Id));
    }

    [Fact]
    public async Task Another_process_reads_and_drives_the_window()
    {
        var powershell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        Assert.SkipUnless(File.Exists(powershell), "Windows PowerShell 5.1 is not installed.");
        using var harness = Open(typeof(Settings));
        var script = Path.Combine(AppContext.BaseDirectory, "Scripts", "UiaClient.ps1");
        var start = new System.Diagnostics.ProcessStartInfo(powershell, $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{script}\" -Handle {harness.Handle} -ToggleId active -InvokeId save")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = System.Diagnostics.Process.Start(start)!;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errors = process.StandardError.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);
        var lines = (await output).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.True(process.ExitCode == 0, await errors);

        Assert.Equal("window|ControlType.Window|Impostazioni", lines[0]);
        Assert.Equal(
            [
                "child|ControlType.CheckBox|Attivo|active|Nazor|True",
                "child|ControlType.Button|Salva le impostazioni|save|Nazor|True",
                "child|ControlType.Group|Riepilogo||Nazor|True",
                "child|ControlType.Pane||list|Nazor|True",
            ],
            lines.Where(l => l.StartsWith("child|", StringComparison.Ordinal) && l.Contains("|Nazor|", StringComparison.Ordinal)));
        var toggle = lines.Single(l => l.StartsWith("toggle|", StringComparison.Ordinal)).Split('|');
        Assert.Equal("On", toggle[1]);
        Assert.True(int.Parse(toggle[2], System.Globalization.CultureInfo.InvariantCulture) < 1000, $"Toggle took {toggle[2]} ms.");
        Assert.True(harness.Read(w => ((Settings)w.Root.Component).Active));
        harness.WaitUntil(w => ((Settings)w.Root.Component).Saves == 1, "the invoked handler");

        var events = lines.Where(l => l.StartsWith("event|", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["event|InvokePatternIdentifiers.InvokedEvent", "event|property|TogglePatternIdentifiers.ToggleStateProperty|1"], events);
    }

    [Fact]
    public void Calls_from_other_threads_are_carried_to_the_ui_thread()
    {
        using var harness = Open(typeof(Form));
        UiaClient.Run(uia => uia.ElementFromHandle(harness.Handle).GetCurrentName());
        var provider = harness.Read(w => ((Win32Window)w.PlatformWindow).Automation!.Wrap(w.Semantics.Descendants().Single(n => n.AutomationId == "name-input")));

        ((IValueProvider)provider).SetValue("da un altro thread");
        Assert.Equal("da un altro thread", ((IValueProvider)provider).GetValue());
        Assert.Equal("da un altro thread", harness.Read(w => ((Form)w.Root.Component).Name));

        harness.Close();
        var error = Assert.Throws<COMException>(() => ((IValueProvider)provider).GetValue());
        Assert.Equal(Uia.E_ELEMENTNOTAVAILABLE, error.HResult);
    }

    [Fact]
    public void Removed_elements_report_element_not_available()
    {
        using var harness = Open(typeof(Settings));
        var error = UiaClient.Run(uia =>
        {
            var row = uia.Find(harness.Handle, "row20");
            Assert.Equal("Riga 20", row.GetCurrentName());
            harness.Read(w =>
            {
                var settings = (Settings)w.Root.Component;
                settings.Rows = 10;
                settings.Refresh();
                return true;
            });
            harness.WaitUntil(w => w.Root.Elements("TextBlock").Count() == 11, "the shorter list");
            try
            {
                _ = row.GetCurrentName();
                return 0;
            }
            catch (COMException exception)
            {
                return exception.HResult;
            }
        });
        Assert.Equal(Uia.E_ELEMENTNOTAVAILABLE, error);
    }
}
