using Nazor.Controls.Tests.Feedback.Fixtures;
using Nazor.Style;

namespace Nazor.Controls.Tests.Feedback;

[Collection("Controls")]
public sealed class StatusTests : IDisposable
{
    private readonly ControlHarness h = new(width: 640, height: 900);

    public void Dispose() => h.Dispose();

    private static ElementNode Part(ElementNode element, string name) => element.Elements().Single(e => e.Name == name);

    private static ElementNode? Find(ElementNode element, string name) => element.Elements().FirstOrDefault(e => e.Name == name);

    [Fact]
    public void A_badge_decorates_its_content_with_a_count_at_a_corner()
    {
        h.Render<StatusProbe>();
        var badge = h.ByClass("count");
        Assert.Equal(true, badge.Attributes["decorating"]);
        Assert.Equal("top-end", badge.Attributes["corner"]);
        var content = Part(badge, "BadgeContent");
        var indicator = Part(badge, "BadgeIndicator");
        Assert.Equal("5", indicator.Text());
        Assert.Equal("text", indicator.Attributes["role"]);
        Assert.Equal("5 new", indicator.Attributes["accessible-name"]);
        var box = h.Bounds(content);
        var mark = h.Bounds(indicator);
        Assert.True(mark.X + mark.Width > box.X + box.Width, "The indicator overhangs the end edge.");
        Assert.True(mark.Y < box.Y, "The indicator overhangs the top edge.");
        Assert.Equal(SystemColors.Resolve().AccentColor, h.Style(indicator).BackgroundColor);
    }

    [Fact]
    public void Large_counts_are_capped_and_zero_hides_the_indicator()
    {
        h.Render<StatusProbe>();
        var probe = h.Component<StatusProbe>();
        probe.Count = 150;
        probe.Refresh();
        h.Frame();
        Assert.Equal("99+", Part(h.ByClass("count"), "BadgeIndicator").Text());
        probe.Count = 0;
        probe.Refresh();
        h.Frame();
        Assert.Null(Find(h.ByClass("count"), "BadgeIndicator"));
        Assert.Null(Find(h.ByClass("zero"), "BadgeIndicator"));
        Assert.True(new Badge { Count = 0, ShowZero = true }.IsIndicatorVisible);
        Assert.Equal("9+", new Badge { Count = 10, Max = 9 }.DisplayText);
    }

    [Fact]
    public void Dots_text_badges_and_names_are_supported()
    {
        h.Render<StatusProbe>();
        var dot = Part(h.ByClass("dot"), "BadgeIndicator");
        Assert.Equal(true, dot.Attributes["dot"]);
        Assert.Equal("", dot.Text());
        Assert.Equal("New activity", dot.Attributes["accessible-name"]);
        Assert.Equal("bottom-start", dot.Attributes["corner"]);
        Assert.Equal("success", dot.Attributes["intent"]);
        var label = h.ByClass("label");
        Assert.Equal(true, label.Attributes["standalone"]);
        Assert.Equal("Beta", Part(label, "BadgeIndicator").Text());
        Assert.Equal("3 unread messages", Part(h.ByClass("named"), "BadgeIndicator").Attributes["accessible-name"]);
    }

    [Fact]
    public void A_tag_shows_a_dot_a_live_marker_and_a_remove_button()
    {
        h.Render<StatusProbe>();
        var probe = h.Component<StatusProbe>();
        var tag = h.ByClass("tag");
        Assert.Equal("success", tag.Attributes["intent"]);
        Assert.NotNull(Find(tag, "TagDot"));
        Assert.Equal("Active", Part(tag, "TagLabel").Text());
        Assert.Null(Find(tag, "TagRemove"));
        var live = h.ByClass("live");
        Assert.Equal(true, live.Attributes["in-progress"]);
        Assert.Equal("in progress", Part(live, "TagLabel").Attributes["accessible-description"]);
        Assert.Equal(BorderStyle.Dashed, h.Style(live).Get(StyleProperties.BorderTopStyle));
        var remove = Part(h.ByClass("removable"), "TagRemove");
        Assert.Equal("button", remove.Attributes["role"]);
        Assert.Equal("Remove", remove.Attributes["accessible-name"]);
        h.Click(remove);
        Assert.Equal(1, probe.Removed);
        h.Focus(Part(h.ByClass("removable"), "TagRemove"));
        h.Press("Delete");
        Assert.Equal(2, probe.Removed);
        h.Press("Enter");
        Assert.Equal(3, probe.Removed);
    }

    [Fact]
    public void A_disabled_tag_ignores_removal()
    {
        h.Render<StatusProbe>();
        var probe = h.Component<StatusProbe>();
        var locked = h.ByClass("locked");
        Assert.True(locked.IsDisabled);
        h.Click(Part(locked, "TagRemove"));
        Assert.Equal(0, probe.Removed);
    }

    [Fact]
    public void An_empty_state_is_a_group_named_by_its_title()
    {
        h.Render<StatusProbe>();
        var probe = h.Component<StatusProbe>();
        var empty = h.ByClass("empty");
        var title = Part(empty, "EmptyStateTitle");
        Assert.Equal("group", empty.Attributes["role"]);
        Assert.Equal(title.Id, empty.Attributes["labeled-by"]);
        Assert.Equal("heading", title.Attributes["role"]);
        Assert.Equal(2, title.Attributes["level"]);
        Assert.Equal("No results", title.Text());
        Assert.Equal("Try another search.", Part(empty, "EmptyStateDescription").Text());
        Assert.NotNull(Find(empty, "EmptyStateIllustration"));
        h.Click(h.ByClass("clear"));
        Assert.Equal(1, probe.Cleared);
        Assert.True(h.Bounds(Part(empty, "EmptyStateIllustration")).Y < h.Bounds(title).Y);
    }

    [Fact]
    public void Skeletons_are_hidden_from_assistive_technologies_and_shaped()
    {
        h.Render<StatusProbe>();
        var lines = h.ByClass("lines");
        Assert.Equal(true, lines.Attributes["semantic-hidden"]);
        Assert.Equal("text", lines.Attributes["shape"]);
        var parts = lines.Elements().ToList();
        Assert.Equal(3, parts.Count);
        Assert.Equal(true, parts[2].Attributes["last"]);
        Assert.True(h.Bounds(parts[2]).Width < h.Bounds(parts[0]).Width);
        var circle = h.ByClass("circle");
        Assert.Equal("circle", circle.Attributes["shape"]);
        Assert.Equal(40, h.Bounds(circle).Width, 1);
        Assert.Equal(40, h.Bounds(circle).Height, 1);
        Assert.Empty(circle.Elements());
        Assert.Equal(20, h.Bounds(h.ByClass("rect")).Height, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => ProgressTests.Validate(new Skeleton { Lines = 0 }));
    }

    [Fact]
    public void An_avatar_shows_initials_and_names_itself_with_its_presence()
    {
        h.Render<StatusProbe>();
        var ada = h.ByClass("ada");
        Assert.Equal("image", ada.Attributes["role"]);
        Assert.Equal("Ada Lovelace, online", ada.Attributes["accessible-name"]);
        Assert.Equal("AL", Part(ada, "AvatarInitials").Text());
        Assert.Equal("online", Part(ada, "AvatarPresence").Attributes["presence"]);
        Assert.Equal("circle", ada.Attributes["shape"]);
        Assert.Equal(32, h.Bounds(ada).Width, 1);
        var square = h.ByClass("initials");
        Assert.Equal("XY", Part(square, "AvatarInitials").Text());
        Assert.Equal("square", square.Attributes["shape"]);
        Assert.Equal(48, h.Bounds(square).Width, 1);
        var anon = h.ByClass("anon");
        Assert.NotNull(Find(anon, "AvatarFallback"));
        Assert.Equal(20, h.Bounds(anon).Width, 1);
        Assert.Equal("2", Part(Part(Part(h.ByClass("badged"), "AvatarBadge"), "Badge"), "BadgeIndicator").Text());
    }

    [Theory]
    [InlineData("Ada Lovelace", "AL")]
    [InlineData("grace", "G")]
    [InlineData("  Edsger W. Dijkstra ", "ED")]
    [InlineData("Ada (Admin)", "A")]
    [InlineData("", null)]
    [InlineData("()", null)]
    public void Initials_come_from_the_first_and_last_words(string name, string? expected) =>
        Assert.Equal(expected, Avatar.InitialsOf(name));

    [Fact]
    public void An_avatar_group_limits_the_visible_avatars_and_counts_the_rest()
    {
        h.Render<StatusProbe>();
        var probe = h.Component<StatusProbe>();
        var team = h.ByClass("team");
        Assert.Equal("group", team.Attributes["role"]);
        Assert.Equal("5 people", team.Attributes["accessible-name"]);
        var avatars = team.Elements().Where(e => e.Name == "Avatar").ToList();
        Assert.Equal(3, avatars.Count);
        Assert.Equal(["first", "middle", "middle"], avatars.Select(a => (string)a.Attributes["position"]!));
        Assert.All(avatars, a => Assert.Equal("small", a.Attributes["size"]));
        var overflow = Part(team, "AvatarGroupOverflow");
        Assert.Equal("+2", overflow.Text());
        Assert.Equal("2 more", overflow.Attributes["accessible-name"]);
        Assert.True(h.Bounds(avatars[1]).X < h.Bounds(avatars[0]).X + h.Bounds(avatars[0]).Width, "Avatars overlap.");
        probe.GroupMax = null;
        probe.Refresh();
        h.Frame();
        team = h.ByClass("team");
        Assert.Equal(5, team.Elements().Count(e => e.Name == "Avatar"));
        Assert.Null(Find(team, "AvatarGroupOverflow"));
        Assert.Equal("last", team.Elements().Last(e => e.Name == "Avatar").Attributes["position"]);
        probe.People.RemoveAt(0);
        probe.GroupMax = 2;
        probe.Refresh();
        h.Frame();
        team = h.ByClass("team");
        Assert.Equal("4 people", team.Attributes["accessible-name"]);
        Assert.Equal(["AT", "GH"], team.Elements().Where(e => e.Name == "Avatar").Select(a => Part(a, "AvatarInitials").Text()));
        Assert.Equal("+2", Part(team, "AvatarGroupOverflow").Text());
    }
}
