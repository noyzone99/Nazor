using Nazor.Graphics.Text;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Skia;
using Nazor.Testing;

namespace Nazor.Diagnostics.Tests;

internal sealed class DiagnosticsHarness : IDisposable
{
    public DiagnosticsHarness(float width = 320, float height = 200)
    {
        Host = new TestHost();
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fonts");
        Graphics = new SkiaRenderer(new FontOptions
        {
            PrivateFontFiles = [Path.Combine(fonts, "FiraSans-Regular.ttf"), Path.Combine(fonts, "FiraSans-Bold.ttf")],
            UseSystemFonts = false,
            DefaultFamily = "Fira Sans",
        });
        Scene = new Scene(Host.Renderer, Graphics.Fonts, Graphics.Shaper, images: Graphics.Images) { Viewport = new Size(width, height) };
        Scene.Styles.Theme = Nazor.Themes.Themes.Basic;
        Scene.Transitions.ReducedMotion = true;
        Input = new InputManager(Host.Renderer, Scene);
        Session = new DevToolsSession(Host.Renderer, Scene, Input);
    }

    public TestHost Host { get; }
    public SkiaRenderer Graphics { get; }
    public Scene Scene { get; }
    public InputManager Input { get; }
    public DevToolsSession Session { get; }

    public void Dispose()
    {
        Session.Dispose();
        Input.Dispose();
        Scene.Dispose();
        Host.Dispose();
        Graphics.Dispose();
    }
}
