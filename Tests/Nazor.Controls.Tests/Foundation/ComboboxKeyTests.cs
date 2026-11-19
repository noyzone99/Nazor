using Nazor.Controls.Primitives;
using Nazor.Input;

namespace Nazor.Controls.Tests.Foundation;

[Collection("Controls")]
public sealed class ComboboxKeyTests : IDisposable
{
    private readonly ControlHarness h = new();
    public void Dispose() => h.Dispose();

    [Theory]
    [InlineData(KeyModifiers.Control)]
    [InlineData(KeyModifiers.Shift)]
    [InlineData(KeyModifiers.Alt)]
    [InlineData(KeyModifiers.Meta)]
    public async Task Modified_Enter_does_not_choose_a_highlighted_suggestion(KeyModifiers modifiers)
    {
        h.Render<Combobox<string>>();
        var combo = h.Component<Combobox<string>>();
        var chosen = new List<string>();
        await combo.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Combobox<string>.Items)] = new[] { "Rome" },
            [nameof(Combobox<string>.ItemText)] = (Func<string, string>)(item => item),
            [nameof(Combobox<string>.OnSelect)] = EventCallback.Factory.Create<string>(combo, chosen.Add),
        }));
        h.Frame();
        h.Focus(h.One("ComboboxInput"));
        h.Press("ArrowDown");
        h.Press("Enter", modifiers);
        Assert.Empty(chosen);
        Assert.True(combo.Opened);
        Assert.Equal(0, combo.CurrentIndex);
        h.Press("Enter");
        Assert.Equal(["Rome"], chosen);
        Assert.False(combo.Opened);
    }

    [Fact]
    public void Alt_arrow_keys_keep_the_explicit_open_and_close_contract()
    {
        h.Render<Combobox<string>>();
        var combo = h.Component<Combobox<string>>();
        h.Focus(h.One("ComboboxInput"));
        h.Press("ArrowDown", KeyModifiers.Alt);
        Assert.True(combo.Opened);
        h.Press("ArrowUp", KeyModifiers.Alt);
        Assert.False(combo.Opened);
    }
}
