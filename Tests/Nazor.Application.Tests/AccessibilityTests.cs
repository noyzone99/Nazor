using Microsoft.Extensions.DependencyInjection;
using Nazor.Graphics.Text;
using System.Text;
using Nazor.Accessibility;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Application.Tests;

public sealed class AccessibilityTests : IDisposable
{
    private readonly ServiceProvider services = new ServiceCollection().AddSingleton<KeyBindings>().BuildServiceProvider();
    private readonly TestHost host;
    private readonly Nazor.Animation.ManualAnimationClock clock = new();
    private readonly SkiaRenderer graphics = new(new FontOptions
    {
        PrivateFontFiles = [Path.Combine(AppContext.BaseDirectory, "Fonts", "FiraSans-Regular.ttf")],
        UseSystemFonts = false,
        DefaultFamily = "Fira Sans",
    });
    private readonly Scene scene;
    private readonly InputManager input;
    private readonly SemanticTree tree;
    private readonly List<string> events = [];

    public AccessibilityTests()
    {
        host = new TestHost(services);
        scene = new Scene(host.Renderer, graphics.Fonts, graphics.Shaper, clock: clock) { Viewport = new Size(320, 240) };
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        input = new InputManager(host.Renderer, scene, services.GetRequiredService<KeyBindings>());
        tree = new SemanticTree(host.Renderer, scene, input);
        tree.PropertyChanged += (node, property, before, after) => events.Add($"{Label(node)}.{property}: {before} -> {after}");
        tree.ChildrenChanged += node => events.Add($"{Label(node)}.Children");
        tree.NodeRemoved += node => events.Add($"removed {Label(node)}");
        tree.FocusChanged += node => events.Add($"focus {(node is null ? "none" : Label(node))}");
        tree.Invoked += node => events.Add($"invoked {Label(node)}");
        tree.LiveRegionChanged += node => events.Add($"live {Label(node)}: {node.Name}");
        tree.Announced += (node, message, priority) => events.Add($"announce {message} ({priority}) from {Label(node)}");
    }

    public void Dispose()
    {
        tree.Dispose();
        input.Dispose();
        scene.Dispose();
        host.Dispose();
        services.Dispose();
        graphics.Dispose();
    }

    [Fact]
    public void Automatic_window_scrolling_is_exposed_on_the_window_without_an_extra_semantic_group()
    {
        Render<AutomaticScrollWindow>();
        var child = Assert.Single(tree.Root.Children);
        Assert.Equal("Scroll content", child.Name);
        Assert.True(tree.Root.Scroll!.CanScrollY);
        Assert.True(tree.ScrollBy(tree.Root, 75));
        Assert.Equal(75, tree.Root.Scroll.OffsetY);
        Assert.True(tree.ScrollTo(tree.Root, null, 0));
        Assert.Equal(0, tree.Root.Scroll.OffsetY);
        scene.Viewport = new Size(320, 1000);
        tree.Update();
        Assert.False(tree.Root.Scroll.CanScrollY);
        Assert.Same(child, Assert.Single(tree.Root.Children));
    }

    public sealed class AutomaticScrollWindow : WindowComponentBase
    {
        protected override void BuildWindowContent(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "TextBlock");
            builder.AddAttribute(1, "style", "height: 800px; flex-shrink: 0");
            builder.AddAttribute(2, "role", "text");
            builder.AddContent(3, "Scroll content");
            builder.CloseElement();
        }
    }

    private static string Label(SemanticNode node) => node.AutomationId ?? node.Name;

    private T Render<T>() where T : IComponent
    {
        var root = host.Render<T>();
        tree.Update();
        events.Clear();
        return (T)root.Component;
    }

    private SemanticNode Node(string automationId) => tree.Descendants().Single(n => n.AutomationId == automationId);

    private static CultureRestore Italian()
    {
        var restore = new CultureRestore(System.Globalization.CultureInfo.CurrentUICulture);
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("it-IT");
        return restore;
    }

    private sealed class CultureRestore(System.Globalization.CultureInfo culture) : IDisposable
    {
        public void Dispose() => System.Globalization.CultureInfo.CurrentUICulture = culture;
    }

    private static string Dump(SemanticNode node)
    {
        var text = new StringBuilder();
        Write(node, 0);
        return text.ToString();

        void Write(SemanticNode current, int depth)
        {
            text.Append(' ', depth * 2).Append(current.Role).Append(" \"").Append(current.Name).Append('"');
            if (current.AutomationId is { } id) text.Append(" #").Append(id);
            if (!current.IsEnabled) text.Append(" disabled");
            if (current.IsChecked is { } isChecked) text.Append(isChecked ? " checked" : " unchecked");
            if (current.Value is { } value) text.Append(" value=\"").Append(value).Append('"');
            if (current.IsExpanded is { } expanded) text.Append(expanded ? " expanded" : " collapsed");
            if (current.Level is { } level) text.Append($" level {level}");
            if (current.Range is { } range) text.Append($" range={range.Value} [{range.Minimum}, {range.Maximum}] step {range.SmallChange}/{range.LargeChange}{(range.IsReadOnly ? " read-only" : "")}");
            text.Append('\n');
            var shown = current.Children.Count > 5 ? 2 : current.Children.Count;
            foreach (var child in current.Children.Take(shown)) Write(child, depth + 1);
            if (shown < current.Children.Count) text.Append(' ', depth * 2 + 2).Append($"… {current.Children.Count} children\n");
        }
    }

    [Fact]
    public void Controls_are_exposed_with_roles_names_and_states_and_layout_containers_are_skipped()
    {
        Render<Settings>();
        Assert.Equal("""
            Window "Impostazioni"
              CheckBox "Attivo" #active unchecked
              Button "Salva le impostazioni" #save
              Group "Riepilogo"
                Text "Elementi: 30"
              Group "" #list
                Text "Riga 1" #row1
                Text "Riga 2" #row2
                … 30 children

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.True(Node("save").IsFocusable);
        Assert.False(Node("row1").IsFocusable);
        Assert.NotNull(Node("list").Scroll);
    }

    [Fact]
    public void Unnamed_overflow_containers_expose_scroll_actions_even_when_the_bars_are_hidden()
    {
        Render<OverflowPanel>();
        var node = Node("overflow-panel");
        Assert.Equal(SemanticRole.Group, node.Role);
        Assert.Equal("", node.Name);
        Assert.True(node.IsFocusable);
        Assert.True(node.Scroll!.CanScrollX && node.Scroll.CanScrollY);
        Assert.True(tree.ScrollBy(node, 10.5f, 25.25f));
        tree.Update();
        Assert.Equal((10.5f, 25.25f), (node.Scroll.OffsetX, node.Scroll.OffsetY));
        Assert.Contains(events, e => e.StartsWith("overflow-panel.HorizontalScrollPercent", StringComparison.Ordinal));
        Assert.Contains(events, e => e.StartsWith("overflow-panel.VerticalScrollPercent", StringComparison.Ordinal));
        Assert.Contains(node.Children, child => child.Name == "Overflow body");
    }

    [Fact]
    public void Named_groups_keep_their_identity_when_overflow_capability_is_added_and_removed()
    {
        var page = Render<OverflowPanel>();
        page.Named = true;
        page.Overflow = "visible";
        page.Refresh();
        tree.Update();
        var node = Node("overflow-panel");
        Assert.Null(node.Scroll);
        page.Overflow = "scroll";
        page.Refresh();
        tree.Update();
        Assert.Same(node, Node("overflow-panel"));
        Assert.NotNull(node.Scroll);
        Assert.Contains("overflow-panel.ScrollAvailable: False -> True", events);
        page.Overflow = "hidden";
        page.Refresh();
        tree.Update();
        Assert.Same(node, Node("overflow-panel"));
        Assert.Equal("Scroll test", node.Name);
        Assert.Null(node.Scroll);
        Assert.Contains("overflow-panel.ScrollAvailable: True -> False", events);
        Assert.False(tree.ScrollBy(node, 24));
    }

    [Fact]
    public void Layout_only_containers_are_skipped_again_after_scrolling_is_disabled()
    {
        var page = Render<OverflowPanel>();
        var node = Node("overflow-panel");
        page.Overflow = "visible";
        page.Refresh();
        tree.Update();
        Assert.DoesNotContain(tree.Descendants(), n => n.AutomationId == "overflow-panel");
        Assert.True(node.IsDetached);
        Assert.False(tree.ScrollBy(node, 24));
        Assert.Contains(tree.Root.Children, n => n.Name == "Overflow body");
    }

    [Fact]
    public void Fitting_content_retains_the_capability_and_reports_new_extents_when_it_overflows()
    {
        var page = Render<OverflowPanel>();
        page.Fits = true;
        page.Refresh();
        tree.Update();
        var node = Node("overflow-panel");
        Assert.NotNull(node.Scroll);
        Assert.False(node.Scroll.CanScroll);
        Assert.False(tree.ScrollBy(node, 24));
        page.Fits = false;
        page.Refresh();
        tree.Update();
        Assert.Same(node, Node("overflow-panel"));
        Assert.True(node.Scroll.CanScrollX && node.Scroll.CanScrollY);
        Assert.Contains("overflow-panel.HorizontallyScrollable: False -> True", events);
        Assert.Contains("overflow-panel.VerticallyScrollable: False -> True", events);
    }

    [Fact]
    public void Existing_roles_and_actions_are_preserved_when_the_control_also_scrolls()
    {
        var page = Render<OverflowPanel>();
        page.AsButton = true;
        page.Refresh();
        tree.Update();
        var node = Node("overflow-panel");
        Assert.Equal(SemanticRole.Button, node.Role);
        Assert.NotNull(node.Scroll);
        Assert.True(tree.Invoke(node));
        host.RunPending();
        Assert.Equal(1, page.Clicks);
        Assert.True(tree.ScrollBy(node, 24));
    }

    [Fact]
    public void Disabled_containers_reject_accessible_scroll_actions()
    {
        var page = Render<OverflowPanel>();
        page.Disabled = true;
        page.Refresh();
        tree.Update();
        var node = Node("overflow-panel");
        Assert.NotNull(node.Scroll);
        Assert.False(node.IsEnabled);
        Assert.False(node.IsFocusable);
        Assert.False(tree.ScrollBy(node, 24));
        Assert.False(tree.ScrollTo(node, null, 24));
    }

    [Fact]
    public void Absolute_accessible_requests_retarget_smooth_scrolling_without_accumulating_old_destinations()
    {
        var page = Render<OverflowPanel>();
        page.Smooth = true;
        page.Refresh();
        tree.Update();
        var node = Node("overflow-panel");
        Assert.True(tree.ScrollTo(node, null, 100.5));
        clock.Advance(50);
        tree.Update();
        Assert.Equal(25.125f, node.Scroll!.OffsetY);
        Assert.True(tree.ScrollTo(node, null, 40.25));
        clock.Advance(200);
        tree.Update();
        Assert.Equal(40.25f, node.Scroll.OffsetY);
    }

    [Fact]
    public void Images_are_named_by_their_text_alternative_and_decorative_ones_are_hidden()
    {
        Render<Logos>();
        Assert.Equal("""
            Window "Loghi"
              Image "Logo di Nazor" #logo
              Image "Nome esplicito" #named
              Image "Aggiungi" #add

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
    }

    [Fact]
    public void Menus_items_and_tooltips_have_their_roles_and_an_item_can_be_invoked()
    {
        var commands = Render<Commands>();
        commands.OpenMenu();
        host.RunPending();
        tree.Update();
        Assert.Equal("""
            Window "Comandi"
              Button "Salva" #save
              Menu ""
                MenuItem "Copia" #copy
                MenuItem "Incolla" #paste disabled

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.False(tree.Invoke(Node("paste")));
        Assert.True(tree.Invoke(Node("copy")));
        host.RunPending();
        Assert.Equal(1, commands.Copies);
        tree.Update();
        Assert.DoesNotContain(tree.Descendants(), n => n.Role == SemanticRole.Menu);

        input.TooltipDelay = TimeSpan.Zero;
        Assert.True(tree.Focus(Node("save")));
        host.RunUntil(() => input.TooltipTarget is not null);
        host.RunPending();
        tree.Update();
        Assert.Equal("ToolTip \"Salva il file\"\n", Dump(tree.Root.Children[^1]));
    }

    [Fact]
    public void Radio_buttons_and_switches_are_checked_or_not_and_can_be_selected_or_toggled()
    {
        var choices = Render<Choices>();
        Assert.Equal("""
            Window "Scelte"
              Group "Taglia"
                RadioButton "Taglia S" #size-S unchecked
                RadioButton "Taglia M" #size-M checked
                RadioButton "Taglia L" #size-L disabled unchecked
              Switch "Notifiche" #notify unchecked
              Slider "Volume" #volume range=40 [0, 100] step 10/20
              ProgressBar "Download" #download range=30 [0, 60] step 0/0 read-only
              ProgressBar "Attendere" #busy

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.False(tree.SetRangeValue(Node("download"), 40));
        Assert.True(tree.Toggle(Node("notify")));
        Assert.True(choices.Notify);
        Assert.False(tree.Select(Node("notify")));
        Assert.True(tree.SetRangeValue(Node("volume"), 73));
        Assert.Equal(70, choices.Volume);
        Assert.False(tree.SetRangeValue(Node("notify"), 1));
        tree.Update();
        Assert.Contains("volume.RangeValue: 40 -> 70", events);
        events.Clear();
        Assert.False(tree.Select(Node("size-L")));
        Assert.True(tree.Select(Node("size-S")));
        host.RunPending();
        Assert.Equal("S", choices.Size);
        tree.Update();
        Assert.True(Node("size-S").IsChecked);
        Assert.Contains("size-S.IsChecked: False -> True", events);
        Assert.Contains("size-M.IsChecked: True -> False", events);
    }

    [Fact]
    public void A_combo_box_shows_its_value_and_its_list_while_open()
    {
        var picker = Render<CountryPicker>();
        Assert.Equal("""
            Window "Ordine"
              ComboBox "Paese" #country value="Francia" collapsed

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.True(tree.SetExpanded(Node("country"), true));
        host.RunPending();
        tree.Update();
        Assert.False(tree.SetExpanded(Node("country"), true));
        Assert.Equal("""
            ComboBox "Paese" #country value="Francia" expanded
              List ""
                ListItem "Italia" unchecked
                ListItem "Francia" checked
                ListItem "Spagna" disabled unchecked
                ListItem "Germania" unchecked

            """.ReplaceLineEndings("\n"), Dump(Node("country")));
        Assert.Contains("country.IsExpanded: False -> True", events);
        var germany = tree.Descendants().Single(n => n.Name == "Germania");
        Assert.False(tree.Select(tree.Descendants().Single(n => n.Name == "Spagna")));
        Assert.True(tree.Select(germany));
        host.RunPending();
        tree.Update();
        Assert.Equal("DE", picker.Country);
        Assert.Equal("""
            ComboBox "Paese" #country value="Germania" collapsed

            """.ReplaceLineEndings("\n"), Dump(Node("country")));
        Assert.True(germany.IsDetached);
    }

    [Fact]
    public void A_password_box_shows_a_mask_keeps_the_clipboard_out_and_tells_no_value()
    {
        var login = Render<Login>();
        var box = host.Renderer.RootComponents[0].Elements("PasswordFieldInput").Single();
        var clipboard = new MemoryClipboard();
        input.Clipboard = clipboard;
        input.Focus(box, visible: false);
        input.TextInput("s3gr€t");
        Assert.Equal("s3gr€t", login.Password);
        Assert.Equal(new string('●', 6), box.Text());
        Assert.Equal("s3gr€t", box.Attributes["value"]);

        input.KeyDown("a", KeyModifiers.Control, false);
        input.KeyDown("c", KeyModifiers.Control, false);
        input.KeyDown("x", KeyModifiers.Control, false);
        Assert.Null(clipboard.GetText());
        Assert.Equal("s3gr€t", login.Password);
        clipboard.SetText("!");
        input.KeyDown("v", KeyModifiers.Control, false);
        Assert.Equal("!", login.Password);

        input.CompositionUpdate("ab", 2);
        scene.Update();
        Assert.Equal(new string('●', 3), scene.GetEditableLayout(box)!.Text);
        input.CompositionCommit("ab");
        Assert.Equal("!ab", login.Password);

        tree.Update();
        Assert.Equal("""
            Window "Accesso"
              TextBox "" #user-input value="anna"
              TextBox "Password" #password-input value=""
              ToggleButton "Show password" unchecked

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.True(Node("password-input").IsPassword);
        Assert.False(Node("user-input").IsPassword);
        Assert.True(tree.SetValue(Node("password-input"), "nuova"));
        Assert.Equal("nuova", login.Password);
    }

    [Fact]
    public void A_list_exposes_the_rows_it_created_and_a_row_can_be_selected()
    {
        var inbox = Render<Inbox>();
        host.RunPending();
        tree.Update();
        Assert.Equal("""
            Window "Posta"
              List "Messaggi" #messages
                Group ""
                  ListItem "Messaggio 0" unchecked
                  ListItem "Messaggio 1" unchecked
                  … 7 children

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.True(tree.Select(Assert.Single(Node("messages").Children).Children[2]));
        host.RunPending();
        Assert.Equal("Messaggio 2", inbox.Selected);
        tree.Update();
        Assert.True(Assert.Single(Node("messages").Children).Children[2].IsChecked);
    }

    [Fact]
    public void A_tree_exposes_its_rows_with_levels_and_items_expand_without_selecting()
    {
        var folders = Render<FolderTree>();
        host.RunPending();
        tree.Update();
        Assert.Equal("""
            Window "Cartelle"
              Tree "Cartelle" #folders
                Group ""
                  TreeItem "Documenti" unchecked collapsed level 1
                  TreeItem "Immagini" unchecked level 1

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        var documents = Assert.Single(Node("folders").Children).Children[0];
        Assert.False(tree.SetExpanded(Assert.Single(Node("folders").Children).Children[1], true));
        Assert.True(tree.SetExpanded(documents, true));
        host.RunPending();
        tree.Update();
        Assert.Equal("""
            Tree "Cartelle" #folders
              Group ""
                TreeItem "Documenti" unchecked expanded level 1
                TreeItem "Fatture" unchecked level 2
                TreeItem "Contratti" unchecked level 2
                TreeItem "Immagini" unchecked level 1

            """.ReplaceLineEndings("\n"), Dump(Node("folders")));
        Assert.Null(folders.Selected);
        Assert.True(tree.Select(Assert.Single(Node("folders").Children).Children[2]));
        host.RunPending();
        Assert.Equal("Contratti", folders.Selected);
    }

    [Fact]
    public void The_header_of_an_expander_is_a_button_that_opens_and_closes()
    {
        Render<Panels>();
        var header = Assert.Single(tree.Root.Children, node => node.Role == SemanticRole.Button);
        Assert.Equal(SemanticRole.Button,header.Role);
        Assert.Equal("Avanzate",header.Name);
        Assert.False(header.IsExpanded);
        Assert.True(tree.SetExpanded(header,true));
        host.RunPending(); tree.Update();
        Assert.True(header.IsExpanded);
        var content = tree.Root.Children.Single(node=>node.Role==SemanticRole.Group);
        Assert.Equal("Avanzate",content.Name);
        Assert.Same(header,content.LabeledBy);
        Assert.Equal("Dentro",Assert.Single(content.Children).Name);
        Assert.False(tree.SetExpanded(header,true));
    }

    [Fact]
    public void Tabs_are_items_of_a_tab_list_and_selecting_one_shows_its_page()
    {
        var pages = Render<Pages>();
        host.RunPending();
        tree.Update();
        var tabs = tree.Descendants().Single(n => n.Role == SemanticRole.TabList);
        Assert.Equal([(SemanticRole.TabItem, "Uno", true), (SemanticRole.TabItem, "Due", false)],
            tabs.Children.Select(n => (n.Role, n.Name, n.IsChecked)));
        Assert.Contains(tree.Descendants(), n => n.Role == SemanticRole.Text && n.Name == "Pagina Uno");
        Assert.True(tree.Select(tabs.Children[1]));
        host.RunPending();
        Assert.Equal(1, pages.Selected);
        tree.Update();
        Assert.Contains(tree.Descendants(), n => n.Role == SemanticRole.Text && n.Name == "Pagina Due");
        Assert.DoesNotContain(tree.Descendants(), n => n.Role == SemanticRole.Text && n.Name == "Pagina Uno");
    }

    [Fact]
    public void A_date_picker_shows_its_date_and_opens_a_calendar_whose_days_are_selected()
    {
        using var italian = Italian();
        var booking = Render<Booking>();
        Assert.Equal("""
            ComboBox "Arrivo" #arrival-input value="15/10/2026" collapsed

            """.ReplaceLineEndings("\n"), Dump(Node("arrival-input")));
        Assert.True(tree.SetExpanded(Node("arrival-input"), true));
        host.RunPending();
        tree.Update();
        var calendar = tree.Descendants().Single(n => n.Role == SemanticRole.Calendar);
        Assert.Equal(SemanticRole.Calendar, calendar.Role);
        Assert.Equal(
            [(SemanticRole.Button, "Ottobre 2026"), (SemanticRole.Button, "Mese precedente"), (SemanticRole.Button, "Mese successivo"), (SemanticRole.List, "Ottobre 2026")],
            calendar.Children.Select(c => (c.Role, c.Name)));
        var days = calendar.Children[3].Children;
        Assert.Equal(DateTime.DaysInMonth(2026, 10), days.Count);
        Assert.Equal(true, days.Single(d => d.Name == "giovedì 15 ottobre 2026").IsChecked);
        Assert.True(tree.Select(days.Single(d => d.Name == "venerdì 16 ottobre 2026")));
        host.RunPending();
        tree.Update();
        Assert.Equal(new DateOnly(2026, 10, 16), booking.Arrival);
        Assert.Equal("16/10/2026", Node("arrival-input").Value);
        Assert.Equal(false, Node("arrival-input").IsExpanded);
    }

    [Fact]
    public void Menu_bars_have_items_that_open_their_menu_and_tool_bars_group_their_controls()
    {
        var editor = Render<Editor>();
        Assert.Equal("""
            Window "Editor"
              MenuBar "" #menus
                MenuItem "File" #file collapsed
                MenuItem "Modifica" #edit collapsed
              ToolBar "Formato" #tools
                Button "Grassetto"
                Separator ""
                Button "Allinea" collapsed

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.True(tree.SetExpanded(Node("edit"), true));
        host.RunPending();
        tree.Update();
        Assert.Equal("""
            MenuItem "Modifica" #edit expanded
              Menu "Modifica"
                MenuItem "Annulla"

            """.ReplaceLineEndings("\n"), Dump(Node("edit")));
        Assert.True(tree.Invoke(Node("edit").Children[0].Children[0]));
        host.RunPending();
        tree.Update();
        Assert.Equal(["Annulla"], editor.Log);
        Assert.Equal(false, Node("edit").IsExpanded);
        Assert.Empty(Node("edit").Children);
        Assert.True(tree.Invoke(Node("file")));
        host.RunPending();
        tree.Update();
        Assert.Equal(true, Node("file").IsExpanded);
    }

    [Fact]
    public void Split_buttons_run_or_open_and_drop_down_buttons_open_their_menu()
    {
        var commands = Render<SaveCommands>();
        Assert.Equal("""
            Window "Documento"
              SplitButton "Salva" #save collapsed
              Button "Ordina" #sort collapsed

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.True(tree.Invoke(Node("save")));
        host.RunPending();
        Assert.Equal(["save"], commands.Log);

        Assert.True(tree.SetExpanded(Node("save"), true));
        host.RunPending();
        tree.Update();
        Assert.Equal("""
            SplitButton "Salva" #save expanded
              Menu "More options"
                MenuItem "Salva con nome"

            """.ReplaceLineEndings("\n"), Dump(Node("save")));
        Assert.Equal(["save"], commands.Log);
        Assert.True(tree.Invoke(Node("save").Children[0].Children[0]));
        host.RunPending();
        tree.Update();
        Assert.Equal(["save", "Salva con nome"], commands.Log);
        Assert.Empty(Node("save").Children);
        Assert.Equal(false, Node("save").IsExpanded);

        Assert.True(tree.SetExpanded(Node("sort"), true));
        host.RunPending();
        tree.Update();
        Assert.Equal("""
            Button "Ordina" #sort expanded
              Menu ""
                MenuItem "Per nome"

            """.ReplaceLineEndings("\n"), Dump(Node("sort")));
        Assert.True(tree.SetExpanded(Node("sort"), false));
        host.RunPending();
        tree.Update();
        Assert.Empty(Node("sort").Children);
    }

    [Fact]
    public void Links_are_invoked_and_info_bars_are_named_status_bars_with_a_close_button()
    {
        using var italian = Italian();
        var notices = Render<Notices>();
        Assert.Equal("""
            Window "Avvisi"
              Hyperlink "Guida"
              Status "Salvato"
                Image "Operazione riuscita"
                Button "Chiudi"

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.True(tree.Invoke(tree.Root.Children[0]));
        host.RunPending();
        Assert.Equal(1, notices.Clicks);
        Assert.True(tree.Invoke(tree.Root.Children[1].Children.Single(n => n.Role == SemanticRole.Button)));
        host.RunPending();
        Assert.False(notices.Open);
        tree.Update();
        Assert.Single(tree.Root.Children);
    }

    [Fact]
    public void Text_boxes_expose_their_value_but_are_not_named_after_it()
    {
        var form = Render<Form>();
        Assert.Equal("""
            Window ""
              TextBox "" #name-input value="Sensore"
              Text "Nome: Sensore"
              TextBox "" #free-input value=""
              TextBox "" #locked-input disabled value="fisso"

            """.ReplaceLineEndings("\n"), Dump(tree.Root));
        Assert.False(Node("locked-input").IsEnabled);
        Assert.False(tree.SetValue(Node("locked-input"), "x"));

        Assert.True(tree.SetValue(Node("name-input"), "Zoë 😀"));
        Assert.Equal("Zoë 😀", form.Name);
        tree.Update();
        Assert.Contains("name-input.Value: Sensore -> Zoë 😀", events);
        Assert.Contains("Nome: Zoë 😀", tree.Root.Children.Select(n => n.Name));
    }

    [Fact]
    public void Toggle_and_focus_act_now_and_invoke_runs_on_the_dispatcher()
    {
        var settings = Render<Settings>();
        Assert.True(tree.Toggle(Node("active")));
        Assert.True(settings.Active);
        tree.Update();
        Assert.True(Node("active").IsChecked);
        Assert.Contains("active.IsChecked: False -> True", events);

        Assert.True(tree.Invoke(Node("save")));
        Assert.Equal(0, settings.Saves);
        host.RunPending();
        Assert.Equal(1, settings.Saves);
        Assert.Contains("invoked save", events);

        Assert.True(tree.Focus(Node("save")));
        Assert.Equal("save", input.Focused?.Id);
        Assert.True(input.Focused!.States.HasFlag(ElementStates.FocusVisible));
        tree.Update();
        Assert.Same(Node("save"), tree.Focused);
        Assert.True(Node("save").HasFocus);
        Assert.Contains("focus save", events);
        Assert.False(tree.Focus(Node("row1")));

        input.KeyDown("Tab", KeyModifiers.Shift, false);
        tree.Update();
        Assert.Equal("focus active", events[^1]);
    }

    [Fact]
    public void Rows_outside_the_scroll_viewport_are_offscreen_and_follow_scrolling()
    {
        Render<Settings>();
        Assert.False(Node("row1").IsOffscreen);
        Assert.True(Node("row30").IsOffscreen);

        Assert.True(tree.ScrollBy(Node("list"), 10_000));
        tree.Update();
        Assert.True(Node("row1").IsOffscreen);
        Assert.False(Node("row30").IsOffscreen);
        Assert.Contains("row30.IsOffscreen: True -> False", events);
        Assert.True(Node("list").Scroll!.OffsetY > 0);
    }

    [Fact]
    public void Nodes_keep_their_identity_and_removed_elements_detach_them()
    {
        var settings = Render<Settings>();
        var save = Node("save");
        var row20 = Node("row20");
        settings.Rows = 10;
        settings.Refresh();
        tree.Update();

        Assert.Same(save, Node("save"));
        Assert.True(row20.IsDetached);
        Assert.Equal(10, Node("list").Children.Count);
        Assert.Equal(20, events.Count(e => e.StartsWith("removed row", StringComparison.Ordinal)));
        Assert.Contains("list.Children", events);
        Assert.Contains("Elementi: 10.Name: Elementi: 30 -> Elementi: 10", events);
        Assert.False(tree.Invoke(row20));
    }

    [Fact]
    public void Labels_descriptions_and_tooltips_describe_the_controls()
    {
        var page = Render<Described>();
        var sensor = Node("sensor-input");
        Assert.Equal("Nome del sensore", sensor.Name);
        Assert.Same(Node("sensor-label"), sensor.LabeledBy);
        Assert.Equal("Il nome che compare nei report", sensor.Description);
        Assert.Equal("Il nome che compare nei report", sensor.HelpText);
        Assert.Equal(("Aggiorna", null), (Node("refresh").Name, Node("refresh").HelpText));
        Assert.Equal(("Salva", "Salva le modifiche (Ctrl+S)"), (Node("save").Name, Node("save").HelpText));
        Assert.Equal("Salva le modifiche (Ctrl+S)",Node("save").Description);
        Assert.Equal(("", null), (Node("orphan-input").Name, Node("orphan-input").LabeledBy));

        page.Label = "Sensore";
        page.Refresh();
        host.RunPending();
        tree.Update();
        Assert.Equal("Sensore", sensor.Name);
        Assert.Contains("sensor-input.Name: Nome del sensore -> Sensore", events);
    }

    [Fact]
    public void Items_know_their_place_in_their_set()
    {
        Render<Choices>();
        Assert.Equal([(1, 3), (2, 3), (3, 3)], new[] { "size-S", "size-M", "size-L" }.Select(id => (Node(id).PositionInSet, Node(id).SizeOfSet)));
        Assert.Equal((null, null), (Node("notify").PositionInSet, Node("notify").SizeOfSet));
        var switchWord = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "it" ? "interruttore" : "switch";
        Assert.Equal(switchWord, Node("notify").RoleDescription);
        Assert.Null(Node("volume").RoleDescription);
    }

    [Fact]
    public void A_virtualized_list_tells_the_place_of_the_rows_it_created()
    {
        Render<Inbox>();
        host.RunPending();
        tree.Update();
        var rows = Assert.Single(Node("messages").Children).Children;
        Assert.Equal((1, 50), (rows[0].PositionInSet, rows[0].SizeOfSet));
        Assert.Equal((3, 50), (rows[2].PositionInSet, rows[2].SizeOfSet));
    }

    [Fact]
    public void Tree_items_count_the_items_of_their_level()
    {
        Render<FolderTree>();
        host.RunPending();
        tree.Update();
        Assert.True(tree.SetExpanded(Assert.Single(Node("folders").Children).Children[0], true));
        host.RunPending();
        tree.Update();
        Assert.Equal([(1, 2), (1, 2), (2, 2), (2, 2)], Assert.Single(Node("folders").Children).Children.Select(n => (n.PositionInSet, n.SizeOfSet)));

        Render<FlatTree>();
        Assert.Equal([(1, 3), (1, 1), (2, 3), (1, 2), (2, 2), (3, 3)], Node("flat").Children.Select(n => (n.PositionInSet, n.SizeOfSet)));
    }

    [Fact]
    public void Tabs_and_menu_items_are_numbered_among_their_siblings()
    {
        Render<Pages>();
        host.RunPending();
        tree.Update();
        Assert.Equal([(1, 2), (2, 2)], tree.Descendants().Single(n => n.Role == SemanticRole.TabList).Children.Select(n => (n.PositionInSet, n.SizeOfSet)));
        Assert.Null(tree.Root.Children[0].PositionInSet);
    }

    [Fact]
    public void Live_regions_announce_what_appears_or_changes_after_the_first_synchronisation()
    {
        var page = Render<LiveNotices>();
        Assert.Equal((LiveSetting.Polite, "Pronto"), (Node("status").LiveSetting, Node("status").Name));
        Assert.DoesNotContain(events, e => e.StartsWith("live", StringComparison.Ordinal));

        page.Status = "Salvataggio…";
        page.InfoOpen = true;
        page.Refresh();
        host.RunPending();
        tree.Update();
        Assert.Contains("live status: Salvataggio…", events);
        Assert.Equal(LiveSetting.Assertive, Node("connection").LiveSetting);
        Assert.Contains("live connection: Connessione persa: Nuovo tentativo tra 5 secondi", events);

        events.Clear();
        page.Message = "Nuovo tentativo tra 4 secondi";
        page.Refresh();
        host.RunPending();
        tree.Update();
        Assert.Equal(["live connection: Connessione persa: Nuovo tentativo tra 4 secondi"], events.Where(e => e.StartsWith("live", StringComparison.Ordinal)));
        events.Clear();
        tree.Update();
        Assert.Empty(events);
        Assert.Equal(LiveSetting.Off, Node("save").LiveSetting);
    }

    [Fact]
    public void Announcements_come_from_the_focused_node_or_the_window()
    {
        Render<LiveNotices>();
        tree.Announce("Salvato");
        tree.Announce("  ");
        Assert.True(tree.Focus(Node("save")));
        tree.Announce("Connessione persa", AnnouncementPriority.Assertive);
        Assert.Equal(["announce Salvato (Polite) from Notifiche", "announce Connessione persa (Assertive) from save"],
            events.Where(e => e.StartsWith("announce", StringComparison.Ordinal)));
    }

    [Fact]
    public void Containers_know_their_selected_items_and_items_their_container()
    {
        Render<Recipients>();
        host.RunPending();
        tree.Update();
        var list = Node("recipients");
        Assert.True(list.CanSelectMultiple);
        Assert.Equal(["Anna", "Carla"], tree.SelectedItems(list).Select(n => n.Name));
        var bruno = Assert.Single(list.Children).Children[1];
        Assert.Same(list, tree.SelectionContainer(bruno));
        Assert.True(tree.SetSelected(bruno, true));
        host.RunPending();
        Assert.Equal(["Anna", "Bruno", "Carla"], tree.SelectedItems(list).Select(n => n.Name));
        Assert.Null(tree.SelectionContainer(list));

        Render<Pages>();
        host.RunPending();
        tree.Update();
        var tabs = tree.Descendants().Single(n => n.Role == SemanticRole.TabList);
        Assert.False(tabs.CanSelectMultiple);
        Assert.Equal(["Uno"], tree.SelectedItems(tabs).Select(n => n.Name));
    }

    [Fact]
    public void Text_boxes_tell_their_text_caret_selection_lines_and_geometry()
    {
        var form = Render<Form>();
        var name = Node("name-input");
        Assert.Equal("Sensore", tree.TextOf(name));
        Assert.Equal(new TextSelection(7, 7, 7), name.TextSelection);

        Assert.True(tree.SelectText(name, 1, 4));
        tree.Update();
        Assert.Equal(new TextSelection(1, 4, 4), name.TextSelection);
        Assert.Contains(events, e => e.StartsWith("name-input.TextSelection:", StringComparison.Ordinal));
        Assert.True(tree.Focus(name));
        Assert.True(tree.SelectText(name, 1, 4));
        input.TextInput("x");
        host.RunPending();
        tree.Update();
        Assert.Equal("Sxore", form.Name);
        Assert.Equal(new TextSelection(2, 2, 2), name.TextSelection);

        var part = Assert.Single(tree.TextBounds(name, 0, 3));
        var caret = Assert.Single(tree.TextBounds(name, 2, 2));
        Assert.True(part.Width > 0 && caret.Width == 0);
        Assert.True(name.Bounds.Contains(new Point(part.X + 1, part.Y + 1)));
        Assert.Equal(0, tree.TextIndexAt(name, new Point(part.X, part.Y + 2)));
        Assert.Equal([(0, 5)], tree.TextLines(name));
        Assert.False(tree.SelectText(Node("locked-input"), 0, 1));
    }

    [Fact]
    public void A_password_box_has_no_text_to_read_and_lines_break_at_the_width()
    {
        Render<Login>();
        Assert.Null(Node("password-input").TextSelection);
        Assert.Null(tree.TextOf(Node("password-input")));
        Assert.Empty(tree.TextBounds(Node("password-input"), 0, 1));

        var notes = Render<Notes>();
        notes.Note = "Una nota abbastanza lunga da andare a capo\nseconda riga";
        notes.Refresh();
        host.RunPending();
        tree.Update();
        var lines = tree.TextLines(Node("note-input"));
        Assert.True(lines.Count >= 3, $"{lines.Count} lines");
        Assert.Equal(0, lines[0].Start);
        Assert.Equal(notes.Note.Length, lines[^1].End);
        Assert.All(lines.Zip(lines.Skip(1)), pair => Assert.Equal(pair.First.End, pair.Second.Start));
    }

    [Theory]
    [InlineData("en-US", "Close", "switch", "Previous month")]
    [InlineData("it-IT", "Chiudi", "interruttore", "Mese precedente")]
    [InlineData("fr-FR", "Close", "switch", "Previous month")]
    public void The_words_of_the_controls_are_in_the_language_of_the_interface(string language, string close, string toggle, string previous)
    {
        var saved = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(language);
        try
        {
            Render<Notices>();
            Assert.Equal(close, tree.Descendants().Single(n => n.Role == SemanticRole.Status).Children.Single(n => n.Role == SemanticRole.Button).Name);
            Render<Choices>();
            Assert.Equal(toggle, Node("notify").RoleDescription);
            Assert.Equal(previous, CalendarArrows().Previous);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = saved;
        }
    }

    [Fact]
    public void A_date_picker_names_the_navigation_buttons_for_the_month_view()
    {
        using var italian = Italian();
        Assert.Equal(("Mese precedente", "Mese successivo"), CalendarArrows());
    }

    private (string Previous, string Next) CalendarArrows()
    {
        Render<Booking>();
        Assert.True(tree.SetExpanded(Node("arrival-input"), true));
        host.RunPending();
        tree.Update();
        var buttons = tree.Descendants().Where(n => n.Role == SemanticRole.Button && n.Element?.Name is "CalendarPrevious" or "CalendarNext").ToList();
        return (buttons.Single(n => n.Element!.Name == "CalendarPrevious").Name, buttons.Single(n => n.Element!.Name == "CalendarNext").Name);
    }

    [Fact]
    public void Hit_testing_returns_the_deepest_node()
    {
        Render<Settings>();
        var save = Node("save");
        Assert.Same(save, tree.HitTest(new Point(save.Bounds.X + save.Bounds.Width / 2, save.Bounds.Y + save.Bounds.Height / 2)));
        Assert.Same(Node("row2"), tree.HitTest(new Point(Node("row2").Bounds.X + 4, Node("row2").Bounds.Y + 4)));
        Assert.Same(tree.Root, tree.HitTest(new Point(2, 2)));
    }
}
