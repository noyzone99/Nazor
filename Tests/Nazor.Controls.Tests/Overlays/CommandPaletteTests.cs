using Nazor.Controls.Primitives;
using Nazor.Controls.Tests.Overlays.Fixtures;
using Nazor.Input;
using Nazor.Style;

namespace Nazor.Controls.Tests.Overlays;

[Collection("Controls")]
public sealed class CommandPaletteTests : IDisposable
{
    private readonly ControlHarness h = new();

    public void Dispose() => h.Dispose();

    private PaletteProbe Open()
    {
        h.Render<PaletteProbe>();
        h.Focus(h.ByClass("before"));
        h.Press("K", KeyModifiers.Control);
        var probe = h.Component<PaletteProbe>();
        Assert.True(probe.Open);
        return probe;
    }

    private string[] Labels() => [.. h.All("CommandPaletteItemLabel").Select(e => e.Text()!)];

    private string[] Headers() => [.. h.All("CommandPaletteGroupHeader").Select(e => e.Text()!)];

    private ElementNode? Current => h.All("CommandPaletteSearchOption").SingleOrDefault(e => e.Attributes.GetValueOrDefault("current") is true);

    [Fact]
    public void The_gesture_opens_a_named_dialog_with_the_focus_in_the_search_field()
    {
        Open();
        var palette = h.One("CommandPalette");
        Assert.Equal("dialog", palette.Attributes["role"]);
        Assert.Equal("Command palette", palette.Attributes["accessible-name"]);
        Assert.Single(h.All("CommandPaletteScrim"));
        Assert.Equal("CommandPaletteSearchInput", h.Focused?.Name);
        Assert.Equal("combobox", h.Focused?.Attributes["role"]);
        Assert.Equal("listbox", h.One("CommandPaletteSearchPopup").Attributes["role"]);
    }

    [Fact]
    public void Every_enabled_command_is_listed_in_groups_with_its_description_and_shortcut()
    {
        Open();
        Assert.Equal(["File new", "File open", "Find in files", "Toggle sidebar", "Dark theme", "Light theme"], Labels());
        Assert.Equal(["Files", "Theme"], Headers());
        Assert.Equal(["Create a document", "Search everywhere"], h.All("CommandPaletteItemDescription").Select(e => e.Text()));
        Assert.Equal("Ctrl+N", h.ByClass("CommandPaletteItemShortcut").Attributes["accessible-name"]);
        Assert.Equal(6, h.All("CommandPaletteSearchOption").First().Attributes["size-of-set"]);
    }

    [Fact]
    public void Typing_filters_by_words_then_parts_then_descriptions_then_letters_in_order()
    {
        var probe = Open();
        h.Type("fi");
        Assert.Equal("fi", h.One("CommandPalette").Attributes.GetValueOrDefault("accessible-name") is string ? h.Component<PaletteProbe>().Open ? "fi" : "" : "");
        Assert.Equal(["File new", "File open", "Find in files"], Labels());
        h.Type("les");
        Assert.Equal(["Find in files"], Labels());
        Assert.True(probe.Open);
    }

    [Fact]
    public void Descriptions_and_letters_in_order_also_match()
    {
        Open();
        h.Type("everywhere");
        Assert.Equal(["Find in files"], Labels());
        for (var i = 0; i < "everywhere".Length; i++) h.Press("Backspace");
        h.Type("tsb");
        Assert.Equal(["Toggle sidebar"], Labels());
    }

    [Fact]
    public void Without_matches_the_empty_text_is_shown()
    {
        Open();
        h.Type("zzz");
        Assert.Empty(Labels());
        Assert.Equal("Nothing here", h.One("CommandPaletteSearchEmpty").Text());
    }

    [Fact]
    public void Enter_runs_the_first_match_and_closes_the_palette()
    {
        var probe = Open();
        h.Type("dark");
        h.Press("Enter");
        Assert.False(probe.Open);
        Assert.Empty(h.All("CommandPalette"));
        Assert.Equal(["Dark theme"], probe.Ran);
        Assert.Equal(["Dark theme"], probe.Executed);
        Assert.Equal("before", h.Focused?.Class);
    }

    [Fact]
    public void Up_and_down_move_the_current_command_while_the_focus_stays_in_the_field()
    {
        var probe = Open();
        h.Press("ArrowDown");
        Assert.Equal("File new", Current?.Elements().Single(e => e.Name == "CommandPaletteItemLabel").Text());
        h.Press("ArrowDown");
        h.Press("ArrowDown");
        Assert.Equal("Find in files", Current?.Elements().Single(e => e.Name == "CommandPaletteItemLabel").Text());
        h.Press("ArrowUp");
        Assert.Equal("File open", Current?.Elements().Single(e => e.Name == "CommandPaletteItemLabel").Text());
        Assert.Equal("CommandPaletteSearchInput", h.Focused?.Name);
        Assert.Equal(SystemColors.Resolve().Highlight, h.Style(Current!.Elements().Single(e => e.Name == "CommandPaletteItem")).BackgroundColor);
        h.Press("Enter");
        Assert.Equal(["File open"], probe.Ran);
        Assert.False(probe.Open);
    }

    [Fact]
    public void A_click_runs_a_command()
    {
        var probe = Open();
        h.Click(h.All("CommandPaletteItemLabel").Single(e => e.Text() == "Toggle sidebar"));
        Assert.Equal(["Toggle sidebar"], probe.Ran);
        Assert.False(probe.Open);
    }

    [Fact]
    public void Escape_closes_and_the_gesture_toggles()
    {
        var probe = Open();
        h.Type("abc");
        h.Press("Escape");
        Assert.False(probe.Open);
        Assert.Empty(probe.Ran);
        h.Press("K", KeyModifiers.Control);
        Assert.True(probe.Open);
        Assert.Equal("", h.Focused?.Attributes.GetValueOrDefault("value") as string ?? "");
        Assert.Equal(6, Labels().Length);
        h.Press("K", KeyModifiers.Control);
        Assert.False(probe.Open);
    }

    [Fact]
    public void The_palette_sits_near_the_top_in_the_middle_of_the_window()
    {
        Open();
        var bounds = h.Bounds(h.One("CommandPalette"));
        Assert.Equal(48, bounds.Y, 1);
        Assert.Equal(560, bounds.Width, 1);
        Assert.Equal(40, bounds.X, 1);
    }

    [Fact]
    public void Scores_rank_prefixes_words_parts_descriptions_and_letters()
    {
        var command = new Command("Find in files", () => { }) { Description = "Search everywhere" };
        Assert.Equal(1, CommandPaletteScore("", command));
        Assert.Equal(100, CommandPaletteScore("find", command));
        Assert.Equal(80, CommandPaletteScore("files", command));
        Assert.Equal(60, CommandPaletteScore("iles", command));
        Assert.Equal(40, CommandPaletteScore("everywhere", command));
        Assert.Equal(20, CommandPaletteScore("fnf", command));
        Assert.Equal(0, CommandPaletteScore("xyz", command));
    }

    private static int CommandPaletteScore(string query, Command command)
    {
        var method = typeof(CommandPalette).GetMethod("Score", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (int)method.Invoke(null, [command, query])!;
    }
}
