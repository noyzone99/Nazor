using Nazor.Graphics.Text;
using Nazor.Animation;
using Nazor.Graphics;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

internal sealed class RenderHarness : IDisposable
{
    public static readonly string FontDirectory = Path.Combine(AppContext.BaseDirectory, "Fonts");

    public RenderHarness(IServiceProvider? services = null, IAnimationClock? clock = null)
    {
        Host = new TestHost(services, window: services?.GetService(typeof(IWindow)) as IWindow);
        Graphics = new SkiaRenderer(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(FontDirectory, "FiraSans-Regular.ttf"), Path.Combine(FontDirectory, "FiraSans-Bold.ttf")],
            UseSystemFonts = false,
            DefaultFamily = "Fira Sans",
        });
        Scene = new Scene(Host.Renderer, Graphics.Fonts, Graphics.Shaper, images: Graphics.Images, clock: clock);
        Scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        Scene.Transitions.ReducedMotion = true;
    }

    public TestHost Host { get; }
    public SkiaRenderer Graphics { get; }
    public Scene Scene { get; }

    public RenderedImage Render(float width, float height, float scale)
    {
        Scene.Viewport = new Size(width, height);
        Scene.Scale = scale;
        Scene.Update();
        return Graphics.RenderToImage(Scene.BuildDisplayList(), scale, Scene.Background);
    }

    public void Dispose()
    {
        Scene.Dispose();
        Host.Dispose();
        Graphics.Dispose();
    }
}
