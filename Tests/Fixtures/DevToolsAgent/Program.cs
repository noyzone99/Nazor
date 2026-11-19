using System.Text.Json;
using Nazor;
using Nazor.DevTools;
using Nazor.DevTools.Fixtures;
using Nazor.Diagnostics;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Rendering;
using Nazor.Skia;
using Nazor.Style;

var dispatcher = new UIDispatcher();
using var renderer = new Renderer(dispatcher);
using var graphics = new SkiaRenderer();
using var scene = new Scene(renderer, graphics.Fonts, graphics.Shaper) { Viewport = new Size(320, 200) };
using var input = new InputManager(renderer, scene);
var session = new DevToolsSession(renderer, scene, input);
scene.Styles.Theme = InspectionStyles.Theme();
scene.Styles.Application = InspectionStyles.Application();
var frameNumber = 0;
var page = (TreePage)renderer.AddRootComponent<TreePage>().Component;
renderer.AddRootComponent<LargeTree>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Count"] = 700 }));
DevToolsAgent? agent = args.Contains("--disabled") ? null : new(session, args[0]);
Console.WriteLine(agent is null ? "DISABLED" : JsonSerializer.Serialize(agent.Descriptor));
Console.Out.Flush();
var exit = false;
var frozen = false;
_ = Task.Run(() =>
{
    while (Console.ReadLine() is { } command)
    {
        if (command == "resume") { Volatile.Write(ref frozen, false); continue; }
        dispatcher.Post(() =>
        {
            switch (command)
            {
                case "change": page.Version++; page.Show = !page.Show; page.Refresh(); Console.WriteLine("CHANGED"); break;
                case "flood":
                    for (var i = 0; i < 500; i++) { page.Version++; page.Refresh(); }
                    Console.WriteLine("FLOODED"); break;
                case "frames":
                    for (var i = 0; i < 125; i++) session.RecordFrame(++frameNumber, FrameCause.RenderBatch | FrameCause.Explicit, TimeSpan.FromMilliseconds(5),
                        TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(0.25), TimeSpan.FromMilliseconds(1),
                        i == 0 ? 4 : 0, i == 0 ? 2 : 0, [new PixelRect(1, 2, 30, 40)]);
                    Console.WriteLine("FRAMES"); break;
                case "frame":
                    session.RecordFrame(++frameNumber, FrameCause.Explicit, TimeSpan.FromMilliseconds(2), TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromMilliseconds(1),
                        TimeSpan.Zero, TimeSpan.FromMilliseconds(1), 0, 0, []); Console.WriteLine("FRAME"); break;
                case "frame-count": Console.WriteLine(frameNumber); break;
                case "ping": Console.WriteLine("PONG"); break;
                case "pick":
                    scene.Update();
                    var piece = session.CaptureTree().Nodes.First(n => n.Name == "Piece");
                    var box = scene.GetBounds(session.ElementOf(piece.Id)!);
                    var point = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
                    session.PointerMoved(point); session.PointerPressed(point, PointerButton.Primary); session.PointerReleased(point, PointerButton.Primary);
                    Console.WriteLine("PICKED"); break;
                case "escape": session.KeyDown("Escape"); session.KeyUp("Escape"); Console.WriteLine("ESCAPED"); break;
                case "selected": Console.WriteLine(session.SelectedId?.ToString() ?? "NONE"); break;
                case "inspect-mode": Console.WriteLine(session.IsInspecting ? "ON" : "OFF"); break;
                case "highlighted": Console.WriteLine(session.HighlightedId?.ToString() ?? "NONE"); break;
                case "inspect": session.SetInspectMode(true); session.Highlight(session.Roots.First()); session.Select(session.Roots.First()); Console.WriteLine("INSPECTING"); break;
                case "inspection": Console.WriteLine(session.IsInspecting || session.HighlightedId is not null || session.SelectedId is not null ? "ACTIVE" : "CLEAR"); break;
                case "freeze": Volatile.Write(ref frozen, true); break;
                case "detach": agent?.Dispose(); agent = null; Console.WriteLine("DETACHED"); break;
                case "replace":
                    agent?.Dispose(); session.Dispose();
                    session = new(renderer, scene, input); agent = new(session, args[0]);
                    Console.WriteLine(JsonSerializer.Serialize(agent.Descriptor)); break;
                case "exit": exit = true; break;
            }
            Console.Out.Flush();
        });
    }
    dispatcher.Post(() => exit = true);
});
try
{
    while (!exit)
    {
        if (!Volatile.Read(ref frozen))
        {
            dispatcher.RunPending();
            if (Volatile.Read(ref frozen)) { Console.WriteLine("FROZEN"); Console.Out.Flush(); }
        }
        Thread.Sleep(1);
    }
}
finally { agent?.Dispose(); session.Dispose(); }
