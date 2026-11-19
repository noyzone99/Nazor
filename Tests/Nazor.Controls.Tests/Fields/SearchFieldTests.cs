using Nazor.Controls.Tests.Fields.Fixtures;
using Nazor.Input;
using System.Globalization;

namespace Nazor.Controls.Tests.Fields;

public sealed record SearchEntry(string Title, string Detail);

[Collection("Controls")]
public sealed class SearchFieldTests : IDisposable
{
    private readonly ControlHarness h = new(width: 800, height: 600);
    public void Dispose() => h.Dispose();

    private SearchFieldProbe Render(Action<SearchFieldProbe>? setup = null)
    {
        h.Render<SearchFieldProbe>();
        var probe = h.Component<SearchFieldProbe>();
        if (setup is not null) { setup(probe); probe.Refresh(); h.Frame(); }
        h.Focus(h.One("SearchFieldInput"));
        return probe;
    }

    private async Task<SearchField<string>> RenderStrings(Dictionary<string, object?>? parameters = null)
    {
        h.Render<SearchField<string>>();
        var search = h.Component<SearchField<string>>();
        parameters ??= new();
        parameters.TryAdd(nameof(SearchField<string>.Suggestions), new[] { "Rome", "Milan" });
        await search.SetParametersAsync(ParameterView.FromDictionary(parameters));
        h.Frame();
        h.Focus(h.One("SearchFieldInput"));
        return search;
    }

    [Theory]
    [InlineData("en-US", "No results")]
    [InlineData("it-IT", "Nessun risultato")]
    public async Task Unmatched_queries_show_a_localized_nonselectable_empty_row(string culture, string message)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var queries = new List<SearchQuery<string>>();
            var search = await RenderStrings(new()
            {
                [nameof(SearchField<string>.QuerySubmitted)] = EventCallback.Factory.Create<SearchQuery<string>>(this, queries.Add),
            });
            h.Type("Nowhere");
            Assert.True(search.Opened);
            Assert.Empty(h.All("SearchFieldOption"));
            Assert.Equal(message, h.One("SearchFieldEmpty").Text());
            Assert.Equal("text", h.One("SearchFieldEmpty").Attributes["role"]);
            h.Press("ArrowDown"); h.Press("Enter");
            Assert.Equal(new SearchQuery<string>("Nowhere", null, false), Assert.Single(queries));
            Assert.Same(h.One("SearchFieldInput"), h.Focused);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public async Task Empty_text_and_placeholder_are_configurable_and_rich_empty_content_takes_precedence()
    {
        var search = await RenderStrings(new()
        {
            [nameof(SearchField<string>.Placeholder)] = "Find a destination",
            [nameof(SearchField<string>.EmptyText)] = "No matching destinations",
        });
        Assert.Equal("Find a destination", h.One("SearchFieldInput").Attributes["placeholder-text"]);
        h.Type("Nowhere");
        Assert.Equal("No matching destinations", h.One("SearchFieldEmpty").Text());
        await search.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SearchField<string>.EmptyContent)] = (RenderFragment)(b => b.AddContent(0, "Try another city")),
        }));
        h.Frame();
        Assert.Equal("Try another city", h.One("SearchFieldEmpty").Text());
    }

    [Fact]
    public async Task Highlighting_preserves_original_text_and_the_selected_value_and_can_be_disabled()
    {
        const string value = "Rome rom ROM";
        var search = await RenderStrings(new()
        {
            [nameof(SearchField<string>.Suggestions)] = new[] { value },
        });
        h.Type(" ro ");
        Assert.Equal(new[] { "Ro", "ro", "RO" }, h.All("Mark").Select(e => e.Text()));
        Assert.Equal(value, h.One("SearchFieldOption").Text());
        await search.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SearchField<string>.HighlightMatches)] = false,
        }));
        h.Frame();
        Assert.Empty(h.All("Mark"));
        h.Click(h.One("SearchFieldOption"));
        Assert.Equal(value, search.Text);
    }

    [Theory]
    [InlineData("fr-FR", "é", "Cafe\u0301", "e\u0301")]
    [InlineData("tr-TR", "i", "İzmir", "İi")]
    [InlineData("en-US", "\u00ad", "Rome", "")]
    [InlineData("en-US", "<", "<Rome>", "<")]
    public async Task Highlighting_uses_cultural_match_lengths_and_keeps_text_literal(string culture, string query, string value, string marked)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            await RenderStrings(new() { [nameof(SearchField<string>.Suggestions)] = new[] { value } });
            h.Type(query);
            Assert.Equal(value, h.One("SearchFieldOption").Text());
            Assert.Equal(marked, string.Concat(h.All("Mark").Select(e => e.Text())));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task Custom_item_templates_can_reuse_highlighting_with_an_inline_highlight_template()
    {
        SearchField<string>? search = null;
        search = await RenderStrings(new()
        {
            [nameof(SearchField<string>.ItemTemplate)] = (RenderFragment<string>)(item => b =>
            {
                b.OpenComponent<TextBlock>(0);
                b.AddComponentParameter(1, nameof(TextBlock.ChildContent), search!.RenderHighlightedText(item));
                b.CloseComponent();
            }),
            [nameof(SearchField<string>.HighlightTemplate)] = (RenderFragment<string>)(match => b =>
            {
                b.OpenComponent<Strong>(0);
                b.AddComponentParameter(1, nameof(Strong.ChildContent), (RenderFragment)(text => text.AddContent(0, match)));
                b.CloseComponent();
            }),
        });
        h.Type("ro");
        Assert.Equal("Ro", h.One("Strong").Text());
        Assert.Empty(h.All("Mark"));
        Assert.Equal("Rome", h.One("SearchFieldOption").Text());
        Assert.Throws<ArgumentNullException>(() => search.RenderHighlightedText(null!));
    }

    [Fact]
    public void Edits_choices_and_query_actions_are_distinct_and_templates_preserve_typed_items()
    {
        var probe = Render();
        h.Type("Ro");
        Assert.Equal(["Ro"], probe.Edited);
        var option = h.One("SearchFieldOption");
        Assert.Contains("Italy", option.Text());
        Assert.Equal("combobox", h.One("SearchFieldInput").Attributes["role"]);
        h.Click(option);
        Assert.Equal("Rome", probe.Value);
        Assert.Equal([new SearchEntry("Rome", "Italy")], probe.Chosen);
        Assert.Equal(["Ro"], probe.Edited);
        Assert.Empty(probe.Queries);
        Assert.Empty(probe.Searches);
        h.Click(h.One("SearchFieldMark"));
        Assert.Equal([new SearchQuery<SearchEntry>("Rome", null, false)], probe.Queries);
        Assert.Equal(["Rome"], probe.Searches);
        Assert.Same(h.One("SearchFieldInput"), h.Focused);
    }

    [Fact]
    public void Enter_with_a_highlighted_suggestion_chooses_and_submits_it_once()
    {
        var probe = Render();
        h.Type("R");
        h.Press("ArrowDown");
        h.Press("Enter");
        Assert.Equal([new SearchEntry("Rome", "Italy")], probe.Chosen);
        Assert.Equal([new SearchQuery<SearchEntry>("Rome", probe.Chosen.Single(), true)], probe.Queries);
        Assert.Equal(["Rome"], probe.Searches);
        Assert.False(probe.Search.Opened);
        h.Press("Enter");
        Assert.Equal(2, probe.Queries.Count);
        Assert.False(probe.Queries[1].HasChosenSuggestion);
        Assert.Single(probe.Chosen);
    }

    [Fact]
    public async Task Query_actions_preserve_application_focus_changes_and_accessibility_invocation_does_not_steal_focus()
    {
        var probe = Render(p => p.MoveFocusOnSubmit = true);
        h.Type("Ro");
        h.Click(h.One("SearchFieldMark"));
        Assert.Same(h.ByClass("outside"), h.Focused);
        probe.MoveFocusOnSubmit = false;
        await h.Host.Renderer.DispatchEventAsync(h.One("SearchFieldMark"), "onclick", new PointerEventArgs { IsSynthetic = true });
        h.Frame();
        Assert.Same(h.ByClass("outside"), h.Focused);
        Assert.Equal(2, probe.Queries.Count);
        Assert.Equal(2, probe.Searches.Count);
    }

    [Fact]
    public void Local_filter_uses_model_data_caps_results_and_preserves_empty_content()
    {
        var probe = Render(p => { p.Match = (item, query) => item.Detail.Contains(query, StringComparison.OrdinalIgnoreCase); p.Maximum = 1; });
        h.Type("Italy");
        Assert.Single(h.All("SearchFieldOption"));
        Assert.Equal("Rome", probe.Search.CurrentSuggestions().Single().Title);
        h.Press("Escape");
        Assert.Equal("Italy", probe.Value);
        h.Press("Escape");
        Assert.Equal("", probe.Value);
        h.Type("Nowhere");
        Assert.Equal("No places", h.One("SearchFieldEmpty").Text());
        h.Press("Enter");
        Assert.Equal("Nowhere", probe.Queries.Single().Text);
    }

    [Fact]
    public async Task External_values_do_not_report_edits_and_decorative_marks_still_allow_Enter_queries()
    {
        var probe = Render(p => { p.QueryEnabled = false; p.Value = "Milan"; });
        Assert.Empty(probe.Edited);
        Assert.Equal(true, h.One("SearchFieldMark").Attributes["semantic-hidden"]);
        h.Press("Enter");
        Assert.Equal("Milan", probe.Searches.Single());
        h.Click(h.One("SearchFieldMark"));
        Assert.Single(probe.Searches);
        probe.Disabled = true; probe.Refresh(); h.Frame();
        await probe.Search.SearchAsync();
        Assert.Single(probe.Searches);
    }

    [Fact]
    public async Task Handled_or_modified_Enter_does_not_choose_or_submit()
    {
        var probe = Render();
        h.Type("R"); h.Press("ArrowDown");
        await h.Host.Renderer.DispatchEventAsync(h.One("SearchFieldInput"), "onkeydown", new KeyboardEventArgs { Key = "Enter", Handled = true });
        h.Press("Enter", KeyModifiers.Control);
        Assert.Empty(probe.Queries);
        Assert.Empty(probe.Chosen);
        Assert.True(probe.Search.Opened);
    }

    [Fact]
    public void Async_requests_show_pending_content_ignore_stale_results_and_cap_the_accepted_result()
    {
        var requests = new List<PendingSearch>();
        var probe = Render(p => { p.Maximum = 1; p.Provider = (query, token) => NewRequest(requests, query, token); });
        h.Type("R");
        var old = requests.Last();
        Assert.Equal("Finding places", h.One("SearchFieldEmpty").Text());
        Assert.True(probe.Search.IsLoading);
        h.Type("o");
        var current = requests.Last();
        Assert.True(old.Token.IsCancellationRequested);
        current.Complete(new SearchEntry("Rome", "Italy"), new SearchEntry("Rovigo", "Italy"));
        h.Frame();
        Assert.False(probe.Search.IsLoading);
        Assert.Equal("Rome", probe.Search.CurrentSuggestions().Single().Title);
        old.Complete(new SearchEntry("Rejected", "Old result"));
        h.Frame();
        Assert.Equal("Rome", probe.Search.CurrentSuggestions().Single().Title);
        Assert.True(probe.Search.Opened);
    }

    [Theory]
    [InlineData("escape")]
    [InlineData("blur")]
    [InlineData("disabled")]
    [InlineData("external")]
    public void Dismissal_and_parameter_changes_cancel_pending_results_without_restoring_them(string reason)
    {
        var requests = new List<PendingSearch>();
        var probe = Render(p => p.Provider = (query, token) => NewRequest(requests, query, token));
        h.Type("R");
        var old = requests.Last();
        switch (reason)
        {
            case "escape": h.Press("Escape"); break;
            case "blur": h.Focus(h.ByClass("outside")); break;
            case "disabled": probe.Disabled = true; probe.Refresh(); h.Frame(); break;
            case "external": probe.Value = "Paris"; probe.Refresh(); h.Frame(); break;
        }
        Assert.True(old.Token.IsCancellationRequested);
        old.Complete(new SearchEntry("Rejected", "Old result")); h.Frame();
        Assert.DoesNotContain(probe.Search.CurrentSuggestions(), item => item.Title == "Rejected");
        if (reason != "external") Assert.False(probe.Search.Opened);
        Assert.Empty(probe.Chosen);
        Assert.Empty(probe.Queries);
    }

    [Fact]
    public async Task Async_errors_are_presented_and_a_retry_replaces_the_error_without_losing_focus()
    {
        var requests = new List<PendingSearch>();
        var probe = Render(p => p.Provider = (query, token) => NewRequest(requests, query, token));
        h.Type("R");
        requests.Last().Fail(new InvalidOperationException("Unavailable")); h.Frame();
        Assert.IsType<InvalidOperationException>(probe.Search.SuggestionError);
        Assert.Equal("Try again", h.One("SearchFieldEmpty").Text());
        var retry = probe.Search.RefreshSuggestionsAsync();
        h.Frame();
        Assert.Null(probe.Search.SuggestionError);
        requests.Last().Complete(new SearchEntry("Rome", "Italy")); h.Frame();
        await retry;
        Assert.Null(probe.Search.SuggestionError);
        Assert.Same(h.One("SearchFieldInput"), h.Focused);
        Assert.Equal("Rome", probe.Search.CurrentSuggestions().Single().Title);
    }

    [Fact]
    public void Disposing_the_control_cancels_pending_requests_and_ignores_late_provider_errors()
    {
        var requests = new List<PendingSearch>();
        Render(p => p.Provider = (query, token) => NewRequest(requests, query, token));
        h.Type("R");
        var request = requests.Last();
        h.Host.Renderer.RemoveRootComponent(h.Root);
        Assert.True(request.Token.IsCancellationRequested);
        request.Fail(new InvalidOperationException("Late failure"));
        h.Frame();
    }

    private static Task<IReadOnlyList<SearchEntry>> NewRequest(List<PendingSearch> requests, string query, CancellationToken token)
    {
        var request = new PendingSearch(query, token);
        requests.Add(request);
        return request.Completion.Task;
    }

    private sealed class PendingSearch(string query, CancellationToken token)
    {
        public string Query { get; } = query;
        public CancellationToken Token { get; } = token;
        public TaskCompletionSource<IReadOnlyList<SearchEntry>> Completion { get; } = new();
        public void Complete(params SearchEntry[] items) => Completion.SetResult(items);
        public void Fail(Exception error) => Completion.SetException(error);
    }
}
