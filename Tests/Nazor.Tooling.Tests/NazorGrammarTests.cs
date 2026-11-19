namespace Nazor.Tooling.Tests;

public sealed class NazorGrammarTests
{
    [Fact]
    public void Window_parameters_keep_CSharp_scopes_and_end_at_the_line()
    {
        const string source = "@window (int OrderId, string? Name = null)\n<TextBlock>Order</TextBlock>";
        var tokens = Tokenize(source);
        tokens.Scoped(source, "window", "keyword.control.directive.nazor");
        tokens.Scoped(source, "int", "source.cs");
        tokens.Scoped(source, "OrderId", "source.cs");
        tokens.Scoped(source, "TextBlock", "entity.name.tag.nazor");
    }
    [Fact]
    public void Native_resize_grip_does_not_leak_out_of_embedded_styles()
    {
        const string source = """
            <Window />
            @style { :root { -window-resize-grip: auto; color: red; } }
            @code { private int count = 1; }
            <TextBlock>@count</TextBlock>
            """;
        var tokens = Tokenize(source);
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
        tokens.Scoped(source, "-window-resize-grip", "support.type.property-name.nss");
        tokens.Scoped(source, "auto", "support.constant.property-value.nss");
        tokens.Scoped(source, "int", "source.cs");
        tokens.Scoped(source, "TextBlock", "entity.name.tag.nazor");
    }

    private static List<Token> Tokenize(string source)
    {
        var (tokens, depth) = GrammarHarness.Instance.Tokenize("text.nazor", source);
        Assert.Equal(1, depth);
        return tokens;
    }

    [Fact]
    public void Markup_has_tags_attributes_and_directive_attributes()
    {
        const string source = """
            @using DeviceRegistry.Components

            <Window Title="Nazor" Width="960">
                <Input Id="filter" @bind-Value="filter" AccessibleName="Filtra" />
                <Button @onclick="Increment">Clic</Button>
            </Window>
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "using", "keyword.control.directive.nazor");
        tokens.Scoped(source, "DeviceRegistry.Components", "entity.name.type.namespace.nazor");
        tokens.Scoped(source, "Window", "entity.name.tag.nazor");
        tokens.Scoped(source, "Title", "entity.other.attribute-name.nazor");
        tokens.Scoped(source, "Nazor", "string.quoted.double.nazor", occurrence: 1);
        tokens.Scoped(source, "bind-Value", "entity.other.attribute-name.directive.nazor");
        tokens.Scoped(source, "filter", "source.cs", occurrence: 2);
        tokens.Scoped(source, "onclick", "entity.other.attribute-name.directive.nazor");
        tokens.Scoped(source, "Increment", "source.cs");
        tokens.NotScoped(source, "Clic", "source.cs");
        tokens.Scoped(source, "Window", "entity.name.tag.nazor", occurrence: 2);
    }

    [Fact]
    public void Lambdas_and_assignments_in_directive_attributes_end_at_the_quote()
    {
        const string source = """
            <Container @oncontextmenu="e => menu?.ShowAsync(e) ?? Task.CompletedTask" Class="area">
                <Button @onclick="() => aperto = !aperto">Altro</Button>
                <Button @onclick="async () => await SalvaAsync()">Salva</Button>
                <Button @onclick="() => { count += 2; }">Più</Button>
                <CheckBox @bind-IsChecked="attivo" @bind-IsChecked:after="() => salvato = attivo is true">Attivo</CheckBox>
            </Container>
            """;
        var tokens = Tokenize(source);
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
        tokens.Scoped(source, "e =>", "entity.name.variable.parameter.cs");
        tokens.Scoped(source, "=>", "keyword.operator.arrow.cs");
        tokens.Scoped(source, "Show", "entity.name.function.cs");
        tokens.Scoped(source, "Class", "entity.other.attribute-name.nazor");
        tokens.Scoped(source, "= !aperto", "keyword.operator.assignment.cs");
        tokens.Scoped(source, "Altro", "text.nazor");
        tokens.NotScoped(source, "Altro", "source.cs");
        tokens.Scoped(source, "async", "storage.modifier.async.cs");
        tokens.Scoped(source, "await", "keyword.operator.expression.await.cs");
        tokens.Scoped(source, "+=", "keyword.operator.assignment.compound.cs");
        tokens.NotScoped(source, "Più", "source.cs");
        tokens.Scoped(source, "is true", "keyword.operator.expression.pattern.is.cs");
        tokens.NotScoped(source, "Attivo", "source.cs");
        tokens.Scoped(source, "Container", "entity.name.tag.nazor", occurrence: 2);
    }

    [Fact]
    public void Single_quoted_attributes_keep_arrow_operators_and_double_quotes_inside_the_value()
    {
        const string source = """
            <ActionRow OnClick='() => status = "Done"' Title='A "quoted" title' />
            <GridView ItemText='n => $"Tile {n}"'
                      Class="tiles" />
            <TextBlock Title='@("A title")'>After</TextBlock>
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "=>", "string.quoted.single.nazor");
        tokens.Scoped(source, "Done", "string.quoted.single.nazor");
        tokens.Scoped(source, "quoted", "string.quoted.single.nazor");
        tokens.Scoped(source, "Tile", "string.quoted.single.nazor");
        tokens.Scoped(source, "Class", "entity.other.attribute-name.nazor");
        tokens.Scoped(source, "A title", "string.quoted.double.cs");
        tokens.NotScoped(source, "After", "source.cs");
        tokens.NotScoped(source, "After", "string");
    }

    [Fact]
    public void Single_quoted_directive_attributes_end_after_lambdas_and_csharp_strings()
    {
        const string source = """
            <Button @onclick='() => status = "Done"'
                    @ondblclick='() => status = $"{count} done"'
                    @oncontextmenu='e => ShowMenu(e, "Open")'
                    Class="action">After</Button>
            <TextBlock>@status</TextBlock>
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "=>", "keyword.operator.arrow.cs");
        tokens.Scoped(source, "Done", "string.quoted.double.cs");
        tokens.Scoped(source, "count", "source.cs");
        tokens.Scoped(source, "ShowMenu", "entity.name.function.cs");
        tokens.Scoped(source, "Open", "string.quoted.double.cs");
        tokens.Scoped(source, "Class", "entity.other.attribute-name.nazor");
        tokens.NotScoped(source, "After", "source.cs");
        tokens.NotScoped(source, "After", "string");
        tokens.Scoped(source, "status", "meta.expression.implicit.nazor", occurrence: 3);
    }

    [Fact]
    public void Interpolated_strings_in_attribute_expressions_keep_text_and_csharp_apart()
    {
        const string source = """
            <CheckBox IsChecked="@Item.IsCompleted" IsCheckedChanged="CompletedChanged" AccessibleName="@($"Completa {Item.Title}")"/>
            <TextBlock Class="riga" Tooltip="@($"{Item.Count} di {Item.Total} ({(Item.Done ? "fatto" : "da fare")})")">Fine</TextBlock>
            <Button AccessibleName="@($"Apri {Item.Title}")" Disabled="@false">Apri</Button>
            """;
        var tokens = Tokenize(source);
        Assert.DoesNotContain(tokens, t => t.Has("invalid"));
        tokens.Scoped(source, "Completa", "string.quoted.double.cs");
        tokens.NotScoped(source, "Completa", "entity.name.tag.nazor");
        tokens.Scoped(source, "Item", "source.cs", occurrence: 2);
        tokens.Scoped(source, "Item", "meta.interpolation.cs", occurrence: 2);
        tokens.Scoped(source, "Item", "variable.other.object.cs", occurrence: 2);
        tokens.Scoped(source, "Title", "source.cs");
        tokens.Scoped(source, "Class", "entity.other.attribute-name.nazor");
        tokens.NotScoped(source, "Class", "source.cs");
        tokens.Scoped(source, " di ", "string.quoted.double.cs");
        tokens.Scoped(source, "\"fatto\"", "string.quoted.double.cs");
        tokens.Scoped(source, "Done", "source.cs");
        tokens.Scoped(source, "Fine", "text.nazor");
        tokens.NotScoped(source, "Fine", "source.cs");
        tokens.NotScoped(source, "Fine", "string.quoted.double.cs");
        tokens.Scoped(source, "Disabled", "entity.other.attribute-name.nazor");
        tokens.NotScoped(source, "Apri</Button>", "source.cs");
    }

    [Fact]
    public void Expressions_are_csharp()
    {
        const string source = """
            <TextBlock>Count: @count and @(dark ? "scuro" : "chiaro") for @device.Name.</TextBlock>
            <Button Disabled="@(!attivo)">x</Button>
            <TextBlock>mail@example.com and @@escaped</TextBlock>
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "@", "keyword.control.nazor.transition");
        tokens.Scoped(source, "count", "meta.expression.implicit.nazor");
        tokens.Scoped(source, "count", "source.cs");
        tokens.NotScoped(source, "and", "source.cs");
        tokens.Scoped(source, "\"scuro\"", "string.quoted.double.cs");
        tokens.Scoped(source, "device", "meta.expression.implicit.nazor");
        tokens.Scoped(source, "Name", "meta.expression.implicit.nazor");
        tokens.NotScoped(source, ".</TextBlock>", "meta.expression.implicit.nazor");
        tokens.Scoped(source, "!", "meta.expression.explicit.nazor");
        tokens.NotScoped(source, "example", "meta.expression.implicit.nazor");
        tokens.Scoped(source, "@@", "constant.character.escape.transition.nazor");
    }

    [Fact]
    public void Control_flow_blocks_contain_markup_and_csharp()
    {
        const string source = """
            @foreach (var device in VisibleDevices)
            {
                <DeviceCard @key="device.Id" Name="@device.Name"
                            Selected="@(selected == device)">
                    <TextBlock>Codice @device.Code</TextBlock>
                </DeviceCard>
            }
            @if (selected is { } current)
            {
                var label = current.Name;
                <TextBlock>@label</TextBlock>
            }
            else
            {
                <TextBlock>Seleziona</TextBlock>
            }
            <Button>Dopo</Button>
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "foreach", "keyword.control.flow.nazor");
        tokens.Scoped(source, "var", "source.cs");
        tokens.Scoped(source, "DeviceCard", "entity.name.tag.nazor");
        tokens.Scoped(source, "key", "entity.other.attribute-name.directive.nazor");
        tokens.Scoped(source, "Selected", "entity.other.attribute-name.nazor");
        tokens.Scoped(source, "TextBlock", "entity.name.tag.nazor");
        tokens.Scoped(source, "Codice", "meta.tag.content.nazor");
        tokens.NotScoped(source, "Codice", "variable.other");
        tokens.Scoped(source, "Code", "meta.expression.implicit.nazor");
        tokens.Scoped(source, "DeviceCard", "entity.name.tag.nazor", occurrence: 2);
        tokens.Scoped(source, "if", "keyword.control.flow.nazor");
        tokens.Scoped(source, "label", "source.cs");
        tokens.Scoped(source, "else", "keyword.control.flow.nazor");
        tokens.Scoped(source, "Seleziona", "meta.tag.content.nazor");
        tokens.Scoped(source, "Button", "entity.name.tag.nazor");
        tokens.NotScoped(source, "Button", "meta.statement");
    }

    [Fact]
    public void Code_and_style_blocks_embed_csharp_and_nss()
    {
        const string source = """
            <Button @onclick="Increment">Increment</Button>

            @style {
                .counter {
                    display: flex;
                    Button { background: #303030; &:hover { background: #0078d4; } }
                }
            }

            @code {
                private int count;
                private void Increment() => count++;
            }
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "style", "keyword.control.directive.style.nazor");
        tokens.Scoped(source, "counter", "entity.other.attribute-name.class.nss");
        tokens.Scoped(source, "display", "support.type.property-name.nss");
        tokens.Scoped(source, "Button", "entity.name.tag.nss", occurrence: 3);
        tokens.Scoped(source, "#303030", "constant.other.color.rgb-value.hex.nss");
        tokens.Scoped(source, ":hover", "entity.other.attribute-name.pseudo-class.nss");
        tokens.Scoped(source, "code", "keyword.control.directive.code.nazor");
        tokens.Scoped(source, "private", "storage.modifier");
        tokens.Scoped(source, "int", "keyword.type");
        tokens.Scoped(source, "Increment", "entity.name.function", occurrence: 3);
    }

    [Fact]
    public void Razor_comments_hide_their_content()
    {
        const string source = """
            @* <Button @onclick="X">not a tag</Button> *@
            <TextBlock>visible</TextBlock>
            """;
        var tokens = Tokenize(source);
        tokens.Scoped(source, "Button", "comment.block.razor.nazor");
        tokens.Scoped(source, "TextBlock", "entity.name.tag.nazor");
    }
}
