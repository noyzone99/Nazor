using Nazor.Routing;

namespace Nazor.Core.Tests;

[Route("/")]
public sealed class HomePage : ComponentBase;

[Route("/ordini")]
public sealed class OrdersPage : ComponentBase;

[Route("/ordini/{Id:int}")]
public sealed class OrderPage : ComponentBase;

[Route("/ordini/nuovo")]
public sealed class NewOrderPage : ComponentBase;

[Route("/clienti/{Name}/{Tab?}")]
[Route("/c/{Name}")]
public sealed class CustomerPage : ComponentBase;

public sealed class NavigationTests
{
    [Fact]
    public async Task Location_changes_report_the_history_operation_for_directional_motion()
    {
        var navigation = new NavigationManager();
        var kinds = new List<NavigationKind>();
        navigation.LocationChanged += (_, args) => kinds.Add(args.Kind);
        await navigation.NavigateToAsync("/one");
        await navigation.NavigateToAsync("/replacement", replace: true);
        await navigation.GoBackAsync();
        await navigation.GoForwardAsync();
        Assert.Equal([NavigationKind.New, NavigationKind.Replace, NavigationKind.Back, NavigationKind.Forward], kinds);
    }

    [Fact]
    public async Task Owned_guards_follow_exact_history_identity_and_do_not_guard_an_outgoing_or_replaced_entry()
    {
        var navigation = new NavigationManager();
        await navigation.NavigateToAsync("/one");
        var owner = navigation.Current;
        var calls = 0;
        var registration = navigation.RegisterLocationChangingHandler(context =>
        {
            calls++;
            if (context.Target.Uri == "/blocked") context.PreventNavigation();
            return ValueTask.CompletedTask;
        }, owner);
        Assert.False(await navigation.NavigateToAsync("/blocked"));
        Assert.Same(owner, navigation.Current);
        Assert.True(await navigation.NavigateToAsync("/two"));
        Assert.True(await navigation.NavigateToAsync("/blocked"));
        Assert.Equal(2, calls);
        Assert.True(await navigation.GoBackAsync());
        Assert.True(await navigation.GoBackAsync());
        Assert.Same(owner, navigation.Current);
        Assert.True(await navigation.NavigateToAsync("/replacement", replace: true));
        Assert.Equal(3, calls);
        Assert.True(await navigation.NavigateToAsync("/blocked"));
        Assert.Equal(3, calls);
        registration.Dispose();
        registration.Dispose();
        Assert.True(await navigation.GoBackAsync());
    }

    [Fact]
    public async Task Equal_entry_values_do_not_share_an_owned_guard_and_global_guards_remain_global()
    {
        var navigation = new NavigationManager();
        await navigation.NavigateToAsync("/same");
        var scoped = 0;
        var global = 0;
        using var registration = navigation.RegisterLocationChangingHandler(_ => { scoped++; return ValueTask.CompletedTask; }, navigation.Current);
        using var application = navigation.RegisterLocationChangingHandler(_ => { global++; return ValueTask.CompletedTask; });
        await navigation.NavigateToAsync("/same");
        await navigation.NavigateToAsync("/other");
        Assert.Equal(1, scoped);
        Assert.Equal(2, global);
        var disposed = navigation.RegisterLocationChangingHandler(_ => { scoped++; return ValueTask.CompletedTask; }, navigation.Current);
        disposed.Dispose();
        disposed.Dispose();
        await navigation.NavigateToAsync("/last");
        Assert.Equal(1, scoped);
        Assert.Equal(3, global);
    }

    private static readonly RouteTable Table = RouteTable.Create([typeof(HomePage), typeof(OrdersPage), typeof(OrderPage), typeof(NewOrderPage), typeof(CustomerPage)]);

    [Fact]
    public void Addresses_match_the_most_specific_route_with_converted_values()
    {
        Assert.Equal(typeof(HomePage), Table.Match("/")!.Page);
        Assert.Equal(typeof(OrdersPage), Table.Match("/Ordini/")!.Page);
        var order = Table.Match("/ordini/42?vista=righe")!;
        Assert.Equal(typeof(OrderPage), order.Page);
        Assert.Equal(42, order.Parameters["id"]);
        Assert.Equal(typeof(NewOrderPage), Table.Match("/ordini/nuovo")!.Page);
        Assert.Null(Table.Match("/ordini/abc"));
        Assert.Null(Table.Match("/ordini/42/righe"));

        var customer = Table.Match("/clienti/Rossi%20Srl")!;
        Assert.Equal(typeof(CustomerPage), customer.Page);
        Assert.Equal("Rossi Srl", customer.Parameters["Name"]);
        Assert.Null(customer.Parameters["Tab"]);
        Assert.Equal("ordini", Table.Match("/clienti/Rossi/ordini")!.Parameters["tab"]);
        Assert.Equal("/c/{Name}", Table.Match("/c/Bianchi")!.Template);
    }

    [Fact]
    public void Bad_templates_are_reported()
    {
        Assert.Throws<FormatException>(() => RouteTable.Create([typeof(BadOptional)]));
        Assert.Throws<FormatException>(() => RouteTable.Create([typeof(BadConstraint)]));
    }

    [Route("/a/{X?}/b")]
    private sealed class BadOptional : ComponentBase;

    [Route("/a/{X:colore}")]
    private sealed class BadConstraint : ComponentBase;

    [Fact]
    public async Task The_history_goes_back_and_forward_and_a_new_place_drops_the_forward_part()
    {
        var navigation = new NavigationManager();
        var changes = new List<string?>();
        navigation.LocationChanged += (_, e) => changes.Add(e.Uri);
        Assert.Equal("/", navigation.Uri);
        Assert.False(navigation.CanGoBack);

        Assert.True(await navigation.NavigateToAsync("ordini/"));
        Assert.True(await navigation.NavigateToAsync("/ordini/42"));
        Assert.True(await navigation.GoBackAsync());
        Assert.Equal("/ordini", navigation.Uri);
        Assert.True(navigation.CanGoForward);
        Assert.True(await navigation.GoForwardAsync());
        Assert.False(await navigation.GoForwardAsync());
        await navigation.GoBackAsync();
        Assert.True(await navigation.NavigateToAsync("/clienti/Rossi"));
        Assert.False(navigation.CanGoForward);
        Assert.True(await navigation.NavigateToAsync("/clienti/Bianchi", replace: true));
        await navigation.GoBackAsync();
        Assert.Equal("/ordini", navigation.Uri);
        Assert.Equal(["/ordini", "/ordini/42", "/ordini", "/ordini/42", "/ordini", "/clienti/Rossi", "/clienti/Bianchi", "/ordini"], changes);

        navigation.NavigateTo<OrderPage>(new Dictionary<string, object?> { ["Id"] = 7 });
        Assert.Null(navigation.Uri);
        Assert.Equal(typeof(OrderPage), navigation.Current.Page);
        Assert.Equal(7, navigation.Current.Parameters!["Id"]);
    }

    [Fact]
    public async Task A_handler_can_stop_a_navigation_also_after_awaiting()
    {
        var navigation = new NavigationManager();
        await navigation.NavigateToAsync("/ordini/1");
        var answer = new TaskCompletionSource<bool>();
        var asked = new List<string?>();
        var registration = navigation.RegisterLocationChangingHandler(async context =>
        {
            asked.Add(context.Target.Uri);
            if (!await answer.Task) context.PreventNavigation();
        });

        var leaving = navigation.NavigateToAsync("/ordini/2");
        Assert.False(leaving.IsCompleted);
        answer.SetResult(false);
        Assert.False(await leaving);
        Assert.Equal("/ordini/1", navigation.Uri);
        Assert.False(await navigation.GoBackAsync());

        registration.Dispose();
        Assert.True(await navigation.GoBackAsync());
        Assert.Equal(["/ordini/2", "/"], asked);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_late_confirmation_cannot_replace_a_newer_navigation(bool replace)
    {
        var navigation = new NavigationManager();
        var confirmed = new TaskCompletionSource();
        using var registration = navigation.RegisterLocationChangingHandler(context =>
            context.Target.Uri == "/slow" ? new ValueTask(confirmed.Task) : ValueTask.CompletedTask);
        var pending = navigation.NavigateToAsync("/slow", replace);
        Assert.True(await navigation.NavigateToAsync("/latest"));
        confirmed.SetResult();
        Assert.False(await pending);
        Assert.Equal("/latest", navigation.Uri);
    }

    [Fact]
    public async Task A_pending_forward_request_cannot_use_an_index_removed_by_a_new_navigation()
    {
        var navigation = new NavigationManager();
        foreach (var uri in new[] { "/one", "/two", "/three" }) await navigation.NavigateToAsync(uri);
        await navigation.GoBackAsync();
        var confirmed = new TaskCompletionSource();
        var registration = navigation.RegisterLocationChangingHandler(_ => new ValueTask(confirmed.Task));
        var pending = navigation.GoForwardAsync();
        registration.Dispose();
        await navigation.GoBackAsync();
        await navigation.NavigateToAsync("/replacement");
        confirmed.SetResult();
        Assert.False(await pending);
        Assert.Equal("/replacement", navigation.Uri);
        Assert.False(navigation.CanGoForward);
    }

    [Fact]
    public async Task A_location_handler_redirect_is_not_overwritten_by_the_original_request()
    {
        var navigation = new NavigationManager();
        using var registration = navigation.RegisterLocationChangingHandler(async context =>
        {
            if (context.Target.Uri == "/private") await navigation.NavigateToAsync("/login");
        });
        Assert.False(await navigation.NavigateToAsync("/private"));
        Assert.Equal("/login", navigation.Uri);
    }
}
