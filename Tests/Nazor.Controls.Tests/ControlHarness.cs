using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Controls.Tests;

internal sealed class ServiceMap(IWindow window, KeyBindings bindings) : IServiceProvider
{
    public NavigationManager Navigation { get; } = new();

    public object? GetService(Type serviceType) =>
        serviceType == typeof(IWindow) ? window : serviceType == typeof(KeyBindings) ? bindings
            : serviceType == typeof(NavigationManager) ? Navigation : null;
}

internal sealed class ControlHarness : IDisposable
{
    public static readonly string FontDirectory = Path.Combine(AppContext.BaseDirectory, "Fonts");

    public ControlHarness(IAnimationClock? clock = null, float width = 640, float height = 480)
    {
        Bindings = new KeyBindings();
        var window = new TestWindow();
        var services = new ServiceMap(window, Bindings);
        Navigation = services.Navigation;
        Host = new TestHost(services, window: window);
        Graphics = new SkiaRenderer(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(FontDirectory, "FiraSans-Regular.ttf"), Path.Combine(FontDirectory, "FiraSans-Bold.ttf")],
            UseSystemFonts = false,
            DefaultFamily = "Fira Sans",
        });
        Scene = new Scene(Host.Renderer, Graphics.Fonts, Graphics.Shaper, images: Graphics.Images, clock: clock)
        {
            Viewport = new Size(width, height),
        };
        Scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        Scene.Transitions.ReducedMotion = true;
        Input = new InputManager(Host.Renderer, Scene, Bindings);
    }

    public TestHost Host { get; }
    public SkiaRenderer Graphics { get; }
    public Scene Scene { get; }
    public InputManager Input { get; }
    public KeyBindings Bindings { get; }
    public NavigationManager Navigation { get; }
    public RootComponent Root { get; private set; } = null!;

    public RootComponent Render<TComponent>() where TComponent : IComponent
    {
        Root = Host.Render<TComponent>();
        Frame();
        return Root;
    }

    public TComponent Component<TComponent>() => (TComponent)Root.Component;

    public void Frame()
    {
        Host.RunPending();
        Scene.Update();
        Host.RunPending();
        Scene.Update();
    }

    public RenderedImage Image(float scale = 1)
    {
        Frame();
        Scene.Scale = scale;
        return Graphics.RenderToImage(Scene.BuildDisplayList(), scale, Scene.Background);
    }

    public IEnumerable<ElementNode> All(string name) => Root.Elements().Where(e => e.Name == name);

    public ElementNode One(string name) => All(name).Single();

    public ElementNode ByClass(string className) => Root.Elements().Single(e => e.Class?.Split(' ').Contains(className) == true);

    public ElementNode? FirstOrDefault(string name) => All(name).FirstOrDefault();

    public Rect Bounds(ElementNode element) => Scene.GetBounds(element);

    public ComputedStyle Style(ElementNode element) => Scene.GetStyle(element);

    public void Click(ElementNode element, KeyModifiers modifiers = KeyModifiers.None, PointerButton button = PointerButton.Primary)
    {
        Frame();
        var bounds = Scene.GetBounds(element);
        var point = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        Input.PointerMoved(point, modifiers);
        Input.PointerPressed(point, button, modifiers);
        Input.PointerReleased(point, button, modifiers);
        Frame();
    }

    public void Press(string key, KeyModifiers modifiers = KeyModifiers.None)
    {
        Input.KeyDown(key, modifiers, false);
        Host.RunPending();
        Input.KeyUp(key, modifiers);
        Frame();
    }

    public void Type(string text)
    {
        Input.TextInput(text);
        Frame();
    }

    public void Focus(ElementNode element)
    {
        Input.Focus(element, visible: true);
        Frame();
    }

    public ElementNode? Focused => Input.Focused;

    public void Dispose()
    {
        Input.Dispose();
        Scene.Dispose();
        Host.Dispose();
        Graphics.Dispose();
    }
}
