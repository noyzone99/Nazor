using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;

namespace Nazor.Rendering.Tests;

public sealed class FluentChoiceSelectionTests
{
    [Theory]
    [InlineData(ColorScheme.Light,1f)] [InlineData(ColorScheme.Light,1.5f)] [InlineData(ColorScheme.Light,2f)]
    [InlineData(ColorScheme.Dark,1f)] [InlineData(ColorScheme.Dark,1.5f)] [InlineData(ColorScheme.Dark,2f)]
    public async Task Hover_and_keyboard_current_do_not_move_the_committed_selection_pill(ColorScheme scheme,float scale)
    {
        using var h=new RenderHarness();using var input=new InputManager(h.Host.Renderer,h.Scene);
        h.Scene.Styles.Theme=Nazor.Themes.Fluent.Themes.Fluent;
        h.Scene.Styles.Preferences=new MediaPreferences(scheme,ReducedMotion:true);
        var root=h.Host.Render<ComboBox<string>>(ParameterView.FromDictionary(new Dictionary<string,object?>
        {
            [nameof(ComboBox<string>.Items)]=new[]{"Rome","Milan","Paris"},
            [nameof(ComboBox<string>.Value)]="Milan",
            [nameof(Control.Style)]="width:220px; margin:20px;",
        }));
        Frame();await ((ComboBox<string>)root.Component).ToggleAsync();Frame();
        var options=root.Elements("ComboBoxOption").ToArray();
        Assert.True(options[1].Attributes.GetValueOrDefault("selected") is true);
        Assert.NotNull(h.Scene.Styles.GetGeneratedElement(options[1],PseudoElement.Before));
        var bounds=h.Scene.GetBounds(options[2]);
        input.PointerMoved(new Point(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2),KeyModifiers.None);Frame();
        options=root.Elements("ComboBoxOption").ToArray();
        Assert.True(options[2].Attributes.GetValueOrDefault("current") is true);
        Assert.False(options[2].Attributes.GetValueOrDefault("selected") is true);
        Assert.NotEqual(0,h.Scene.GetStyle(options[2]).BackgroundColor.A);
        Assert.Null(h.Scene.Styles.GetGeneratedElement(options[2],PseudoElement.Before));
        Assert.True(options[1].Attributes.GetValueOrDefault("selected") is true);
        Assert.NotNull(h.Scene.Styles.GetGeneratedElement(options[1],PseudoElement.Before));
        input.Focus(root.Single("ComboBoxInput"),visible:true);input.KeyDown("ArrowUp",KeyModifiers.None,false);Frame();
        Assert.Equal("Milan",((ComboBox<string>)root.Component).SelectedValue);
        input.KeyDown("ArrowDown",KeyModifiers.None,false);input.KeyDown("Enter",KeyModifiers.None,false);Frame();
        await ((ComboBox<string>)root.Component).ToggleAsync();Frame();
        Assert.Single(root.Elements("ComboBoxOption"),option=>option.Attributes.GetValueOrDefault("selected") is true && option.Text()=="Paris");
        void Frame(){h.Host.RunPending();h.Render(320,300,scale);h.Host.RunPending();h.Render(320,300,scale);}
    }

    [Fact]
    public void Fluent_select_retains_the_accent_pill_and_removes_the_extra_check_slot()
    {
        using var h=new RenderHarness();h.Scene.Styles.Theme=Nazor.Themes.Fluent.Themes.Fluent;
        var root=h.Host.Render<Select<string>>(ParameterView.FromDictionary(new Dictionary<string,object?>
        {
            [nameof(Select<string>.Items)]=new[]{"Rome","Milan"},[nameof(Select<string>.Value)]="Rome",[nameof(Select<string>.IsOpen)]=true,
        }));
        h.Render(320,300,1);
        var selected=Assert.Single(root.Elements("SelectOption"),option=>option.Attributes.GetValueOrDefault("selected") is true);
        var pill=h.Scene.Styles.GetGeneratedElement(selected,PseudoElement.Before);
        Assert.NotNull(pill);Assert.Equal(Length.Px(3),h.Scene.GetStyle(pill).Get(StyleProperties.Width).Length);
        Assert.All(root.Elements("SelectOptionCheck"),check=>Assert.Equal(Display.None,h.Scene.GetStyle(check).Display));
    }
}
