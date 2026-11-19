using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;

namespace Nazor.Samples.Counter;

/// <summary>
/// The Counter without a window: the component tree renders into the retained element tree, styles are resolved, the
/// layout is computed and the surface is drawn with Skia. A click is dispatched to the Button element. With "--png
/// &lt;dir&gt;" the images are saved at 100%, 150% and 200%.
/// </summary>
internal static class Headless
{
    public static int Run(string[] args)
    {
        // A native host normally owns these objects. Creating them explicitly shows the rendering pipeline.
        // Keep component work on this dispatcher even though no window or platform message loop exists.
        var dispatcher = new UIDispatcher();
        using var renderer = new Renderer(dispatcher);
        using var graphics = new SkiaRenderer();
        using var scene = new Scene(renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 200) };
        // Controls need their structural theme in a headless scene too; rendering is independent of a native window.
        scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        // The renderer builds the retained tree; Scene.Update resolves styles and layout before inspection or drawing.
        var root = renderer.AddRootComponent<Counter>();
        scene.Update();
        Print(root.Nodes, 0);
        Save("before");

        var button = Elements(root.Nodes).Single(e => e.Name == "Button");
        // Dispatch directly to test the component callback. A real pointer would first pass through InputManager hit testing.
        var click = renderer.DispatchEventAsync(button, "onclick", new PointerEventArgs { Button = PointerButton.Primary });
        // Pump dispatcher continuations instead of blocking the UI thread on an unfinished asynchronous callback.
        dispatcher.RunUntil(() => click.IsCompleted, TimeSpan.FromSeconds(5));
        scene.Update();
        Console.WriteLine("After one click:");
        Print(root.Nodes, 0);
        Save("after");
        return Elements(root.Nodes).Any(e => e.Name == "TextBlock" && Text(e) == "Count: 1") ? 0 : 1;

        void Save(string name)
        {
            var index = Array.IndexOf(args, "--png");
            if (index < 0 || index + 1 >= args.Length) return;
            Directory.CreateDirectory(args[index + 1]);
            // Logical layout stays the same; scale changes the physical pixel density used for rasterization.
            foreach (var scale in new[] { 1f, 1.5f, 2f })
            {
                scene.Scale = scale;
                scene.Update();
                var image = graphics.RenderToImage(scene.BuildDisplayList(), scale, scene.Background);
                var path = Path.Combine(args[index + 1], $"counter-{name}@{scale:0.#}x.png");
                File.WriteAllBytes(path, graphics.EncodePng(image));
                Console.WriteLine($"Saved {path} ({image.Width}x{image.Height})");
            }
            scene.Scale = 1;
            scene.Update();
        }

        static IEnumerable<ElementNode> Elements(IEnumerable<Node> nodes) =>
            nodes.OfType<ElementNode>().SelectMany(e => new[] { e }.Concat(Elements(e.Children)));

        static string Text(Node node) => node is TextNode text ? text.Text : string.Concat(((ElementNode)node).Children.Select(Text));

        void Print(IEnumerable<Node> nodes, int depth)
        {
            foreach (var node in nodes)
            {
                var detail = node is ElementNode element ? $"  {scene.GetBounds(element)}  {{ {scene.GetStyle(element).Describe()} }}" : "";
                Console.WriteLine(new string(' ', depth * 2) + node + detail);
                if (node is ElementNode parent) Print(parent.Children, depth + 1);
            }
        }
    }
}
