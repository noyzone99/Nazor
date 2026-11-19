using Nazor.Controls;
using Nazor.Controls.Primitives;

namespace Nazor.Application.Tests;

public sealed class ControlsPackageBoundaryTests
{
    [Fact]
    public void Controls_and_composition_primitives_share_the_sdk_assembly()
    {
        var assembly = typeof(Button).Assembly;
        Type[] controls = [typeof(TextField), typeof(NumberField), typeof(DatePicker), typeof(SplitView),
            typeof(Tabs), typeof(ListView<string>), typeof(TreeView<string>), typeof(MenuBar), typeof(Controls.Toolbar),
            typeof(Dialog), typeof(TitleBar), typeof(SnapLayoutArea)];
        Assert.All(controls, type =>
        {
            Assert.Same(assembly, type.Assembly);
            Assert.Equal("Nazor.Controls", type.Namespace);
        });
        Assert.Same(assembly, typeof(Pressable).Assembly);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name == "Nazor.Controls.Advanced");
    }
}
