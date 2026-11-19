using Microsoft.Extensions.DependencyInjection;
using Nazor.Accessibility;
using Nazor.Graphics;
using Nazor.Graphics.Text;
using Nazor.Input;
using Nazor.Presentation;
using Nazor.Samples.Gallery;
using Nazor.Skia;
using Nazor.Testing;
namespace Nazor.Application.Tests;
public sealed class BaseUnstyledTests
{
    public static IEnumerable<object[]> Samples => GalleryCatalog.Entries.Select(e=>new object[] {e.Id});
    [Theory]
    [MemberData(nameof(Samples))]
    public async Task Basic_styles_examples_without_Gallery_chrome(string id)
    {
        using var services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
        using var scope = services.CreateScope();
        using var host=new TestHost(scope.ServiceProvider); using var graphics=new SkiaRenderer(new FontOptions {UseSystemFonts=true});
        using var scene=new Scene(host.Renderer,graphics.Fonts,graphics.Shaper,images:graphics.Images) {Viewport=new Size(800,800)};
        scene.Styles.Theme=Themes.Themes.Basic;
        using var input=new InputManager(host.Renderer,scene) {TooltipDelay=TimeSpan.Zero};
        using var tree=new SemanticTree(host.Renderer,scene,input);
        var root=host.Render(GalleryCatalog.Entries.Single(e=>e.Id==id).SampleType);
        void Update() {host.RunPending(); tree.Update();}
        Update();
        foreach(var element in root.Elements().Where(e=>e.Name is "TextFieldInput" or "NumberFieldInput" or "CheckBoxMark" or "Slider" or "Select" or "SelectChevron" or "Separator" or "ProgressBar"))
        {
            var bounds=scene.GetBounds(element); Assert.True(bounds.Width>0 && bounds.Height>0,$"{id}: {element.Name} has no intrinsic geometry: {bounds}");
        }
        Assert.NotEmpty(tree.Descendants());
        if(id == "image")
        {
            var loaded=scene.WhenImagesLoaded(); host.RunUntil(()=>loaded.IsCompleted); await loaded; Update();
            Assert.Contains(scene.BuildDisplayList().Commands,command=>command is DrawImage);
        }
        if (id == "progress-bar")
        {
            var determinate = root.Elements("ProgressBar").Single(e => e.Attributes.GetValueOrDefault("range-value") is 40d);
            var fill = determinate.Elements().Single(e => e.Name == "ProgressBarFill");
            var track = determinate.Elements().Single(e => e.Name == "ProgressBarTrack");
            Assert.InRange(scene.GetBounds(fill).Width / scene.GetBounds(track).Width, 0.39f, 0.41f);
            var indicators = tree.Descendants().Where(node => node.Role == SemanticRole.ProgressBar).ToArray();
            Assert.Equal(3, indicators.Length);
            Assert.Single(indicators, node => node.Range is null);
        }
    }
}
