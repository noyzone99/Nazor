using System.Text;
using Nazor.Input;
using Nazor.Rendering;

namespace Nazor.Testing;

public sealed class TestHost : IDisposable
{
    public TestHost(IServiceProvider? services = null, bool captureExceptions = false, IWindow? window = null)
    {
        Window = window ?? new TestWindow();
        Renderer = new Renderer(Dispatcher, services, Window);
        Renderer.BatchCompleted += (_, batch) => Batches.Add(batch);
        if (captureExceptions)
        {
            Renderer.UnhandledException += (_, exception) => Exceptions.Add(exception);
            Dispatcher.UnhandledException += (_, exception) => Exceptions.Add(exception);
        }
    }

    public UIDispatcher Dispatcher { get; } = new();
    public Renderer Renderer { get; }
    public IWindow Window { get; }
    public List<RenderBatch> Batches { get; } = [];
    public List<Exception> Exceptions { get; } = [];

    public RootComponent Render<TComponent>(ParameterView parameters = default) where TComponent : IComponent =>
        Renderer.AddRootComponent<TComponent>(parameters);

    public RootComponent Render(Type componentType, ParameterView parameters = default) => Renderer.AddRootComponent(componentType, parameters);

    public Task ClickAsync(ElementNode element) =>
        Renderer.DispatchEventAsync(element, "onclick", new PointerEventArgs { Button = PointerButton.Primary });

    public int RunPending() => Dispatcher.RunPending();

    public void RunUntil(Func<bool> condition, int timeoutMilliseconds = 5000)
    {
        if (!Dispatcher.RunUntil(condition, TimeSpan.FromMilliseconds(timeoutMilliseconds)))
            throw new TimeoutException("The condition was not met before the timeout.");
    }

    public void Dispose() => Renderer.Dispose();
}

public static class TreeExtensions
{
    public static IEnumerable<ElementNode> Elements(this RootComponent root) => root.Nodes.SelectMany(Self);

    public static IEnumerable<ElementNode> Elements(this ElementNode element) => element.Children.SelectMany(Self);

    public static IEnumerable<ElementNode> Elements(this RootComponent root, string name) => root.Elements().Where(e => e.Name == name);

    public static ElementNode Single(this RootComponent root, string name) => root.Elements(name).Single();

    public static string Text(this RootComponent root) => string.Concat(root.Nodes.Select(Text));

    public static string Text(this Node node) => node switch
    {
        TextNode text => text.Text,
        ElementNode element => string.Concat(element.Children.Select(Text)),
        _ => "",
    };

    public static string Dump(this RootComponent root)
    {
        var builder = new StringBuilder();
        foreach (var node in root.Nodes) Dump(node, 0, builder);
        return builder.ToString().TrimEnd();
    }

    private static IEnumerable<ElementNode> Self(Node node) =>
        node is ElementNode element ? new[] { element }.Concat(element.Elements()) : [];

    private static void Dump(Node node, int depth, StringBuilder builder)
    {
        builder.Append(' ', depth * 2);
        if (node is TextNode text)
        {
            builder.Append('"').Append(text.Text).Append("\"\n");
            return;
        }
        var element = (ElementNode)node;
        builder.Append(element);
        if (element.Key is not null) builder.Append(" key=").Append(element.Key);
        builder.Append('\n');
        foreach (var child in element.Children) Dump(child, depth + 1, builder);
    }
}
