using Nazor.Nss;
using Nazor.Rendering;
using Nazor.Testing;

namespace Nazor.Style.Tests;

public sealed class HotAlpha : ComponentBase;

public sealed class HotBeta : ComponentBase;

public sealed class HotEpsilon : ComponentBase;

public sealed class HotZeta : ComponentBase;

public sealed class HotDelta : ComponentBase
{
    public sealed class Nested;
}

public sealed class HotGeneric<T> : ComponentBase;

public static class __NazorStyles_HotGeneric
{
    public static int Calls;

    internal static void __NazorRegisterStyleSheet() => Calls++;
}

public static class HotRegistering
{
    public static int Calls;

    internal static void __NazorRegisterStyleSheet() => Calls++;
}

public sealed class HotGamma : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Box>(0, typeof(HotGamma));
        builder.AddComponentParameter(1, "Class", "outer");
        builder.CloseComponent();
    }
}

[Collection("StyleRegistry")]
public sealed class HotReloadTests
{
    private static StyleSheet Sheet(string text)
    {
        var result = NssCompiler.Compile("test", text, "test.nss");
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return result.StyleSheet;
    }

    [Fact]
    public void Replace_keeps_identity_bumps_the_version_and_ignores_equal_rules()
    {
        var sheet = Sheet(".a { padding: 1px; }");
        var registryVersion = StyleRegistry.Version;
        Assert.Equal(0, sheet.Version);

        Assert.False(sheet.Replace(Sheet(".a { padding: 1px; }").Rules));
        Assert.Equal(0, sheet.Version);
        Assert.Equal(registryVersion, StyleRegistry.Version);

        var rules = sheet.Rules;
        var replacement = Sheet(".a { padding: 2px; }");
        Assert.True(sheet.Replace(replacement.Rules));
        Assert.Equal(1, sheet.Version);
        Assert.True(StyleRegistry.Version > registryVersion);
        Assert.Equal(replacement.Describe(), sheet.Describe());
        Assert.Equal(".a (0,1,0) { padding-top: 1px; padding-right: 1px; padding-bottom: 1px; padding-left: 1px }\n",
            new StyleSheet("old", rules).Describe());
        Assert.Equal("test", sheet.Source);
    }

    [Fact]
    public void Registering_a_type_again_replaces_the_created_sheet_in_place()
    {
        StyleRegistry.RegisterScoped(typeof(HotAlpha), () => Sheet(".a { padding: 1px; }"));
        var created = StyleRegistry.GetScoped(typeof(HotAlpha));
        Assert.NotNull(created);
        Assert.Contains(typeof(HotAlpha), StyleRegistry.ScopedTypes);

        StyleRegistry.RegisterScoped(typeof(HotAlpha), () => Sheet(".a { padding: 2px; }"));

        Assert.Same(created, StyleRegistry.GetScoped(typeof(HotAlpha)));
        Assert.Equal(Sheet(".a { padding: 2px; }").Describe(), created.Describe());
        Assert.Equal(1, created.Version);

        var version = StyleRegistry.Version;
        StyleRegistry.ReplaceScoped(typeof(HotAlpha), Sheet(".a { padding: 3px; }"));
        Assert.Same(created, StyleRegistry.GetScoped(typeof(HotAlpha)));
        Assert.Equal(Sheet(".a { padding: 3px; }").Describe(), created.Describe());
        Assert.True(StyleRegistry.Version > version);
        StyleRegistry.Refresh([typeof(HotAlpha)]);
        Assert.Equal(Sheet(".a { padding: 2px; }").Describe(), created.Describe());
    }

    [Fact]
    public void The_first_registration_of_a_type_does_not_bump_the_version()
    {
        var version = StyleRegistry.Version;
        StyleRegistry.RegisterScoped(typeof(HotZeta), () => Sheet(".z { padding: 1px; }"));
        Assert.Equal(version, StyleRegistry.Version);

        var pushed = Sheet(".z { padding: 2px; }");
        StyleRegistry.ReplaceScoped(typeof(HotZeta), pushed);
        Assert.True(StyleRegistry.Version > version);
        Assert.Same(pushed, StyleRegistry.GetScoped(typeof(HotZeta)));
        StyleRegistry.Refresh([typeof(HotZeta)]);
        Assert.Equal(Sheet(".z { padding: 1px; }").Describe(), pushed.Describe());
    }

    [Fact]
    public void Refresh_runs_the_factory_again_and_keeps_the_sheet()
    {
        var text = ".b { padding: 1px; }";
        StyleRegistry.RegisterScoped(typeof(HotBeta), () => Sheet(text));
        var created = StyleRegistry.GetScoped(typeof(HotBeta))!;

        text = ".b { padding: 3px; }";
        StyleRegistry.Refresh([typeof(HotBeta), typeof(string)]);

        Assert.Same(created, StyleRegistry.GetScoped(typeof(HotBeta)));
        Assert.Equal(Sheet(text).Describe(), created.Describe());

        text = ".b { padding: 4px; }";
        StyleRegistry.Refresh(null);
        Assert.Equal(Sheet(text).Describe(), created.Describe());
    }

    [Fact]
    public void Engine_recomputes_after_a_replace_without_being_told()
    {
        using var host = new TestHost();
        using var engine = new StyleEngine(host.Renderer);
        StyleRegistry.RegisterScoped(typeof(HotGamma), () => Sheet(".outer { padding: 1px; }"));
        var root = host.Render<HotGamma>();
        var outer = root.Single("Box");
        Assert.Equal(Length.Px(1), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));
        Assert.False(engine.IsDirty);

        var scoped = StyleRegistry.GetScoped(typeof(HotGamma))!;
        Assert.True(scoped.Replace(Sheet(".outer { padding: 5px; @media (max-width: 100px) { padding: 7px; } }").Rules));

        Assert.True(engine.IsDirty);
        Assert.Equal(Length.Px(5), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));
        Assert.False(engine.IsDirty);

        engine.Viewport = new Viewport(80, 60);
        Assert.Equal(Length.Px(7), engine.GetStyle(outer).Get(StyleProperties.PaddingTop));

        engine.InvalidateAll();
        Assert.True(engine.IsDirty);
        engine.Update();
        Assert.Equal(1, engine.LastRecomputedCount);
    }

    [Fact]
    public void Find_is_empty_while_tracking_is_off()
    {
        Assert.False(StyleRegistry.TrackingEnabled);
        var sheet = new StyleSheet("Styles/Off.nss", []);
        Assert.Empty(StyleRegistry.Find("Styles/Off.nss"));
        GC.KeepAlive(sheet);
    }

    [Fact]
    public void Tracking_finds_live_sheets_and_applies_pending_rules_at_creation()
    {
        StyleRegistry.TrackingEnabled = true;
        try
        {
            var dark = new StyleSheet("Styles/Dark.nss", []);
            Assert.Contains(dark, StyleRegistry.Find("styles\\DARK.nss"));
            Assert.DoesNotContain(dark, StyleRegistry.Find("Styles/Light.nss"));
            Assert.Empty(StyleRegistry.Find("Styles/Missing.nss"));
            var carrier = StyleSheetJson.Deserialize(StyleSheetJson.Serialize(dark));
            Assert.Equal([dark], StyleRegistry.Find("Styles/Dark.nss"));
            GC.KeepAlive(carrier);

            var rules = Sheet(".x { padding: 2px; }");
            StyleRegistry.SetPending("Styles\\Light.nss", rules.Rules);
            var light = new StyleSheet("Styles/Light.nss", []);
            Assert.Equal(rules.Describe(), light.Describe());
            Assert.Equal(1, light.Version);
            Assert.Contains(light, StyleRegistry.Find("Styles/Light.nss"));
            Assert.Equal("", StyleSheet.Empty.Describe());
            GC.KeepAlive(dark);
        }
        finally
        {
            StyleRegistry.SetPending("Styles/Light.nss", []);
            StyleRegistry.TrackingEnabled = false;
        }
    }

    [Fact]
    public void ClearCache_maps_nested_types_to_their_component()
    {
        var text = ".d { padding: 1px; }";
        StyleRegistry.RegisterScoped(typeof(HotDelta), () => Sheet(text));
        var created = StyleRegistry.GetScoped(typeof(HotDelta))!;

        text = ".d { padding: 2px; }";
        StyleHotReload.ClearCache([typeof(HotDelta.Nested)]);

        Assert.Null(StyleHotReload.LastError);
        Assert.Same(created, StyleRegistry.GetScoped(typeof(HotDelta)));
        Assert.Equal(Sheet(text).Describe(), created.Describe());
    }

    [Fact]
    public void ClearCache_invokes_the_registration_method_of_a_component_without_a_sheet()
    {
        var calls = HotRegistering.Calls;
        StyleHotReload.ClearCache([typeof(HotRegistering), typeof(string)]);
        Assert.Equal(calls + 1, HotRegistering.Calls);
        Assert.Null(StyleHotReload.LastError);
    }

    [Fact]
    public void ClearCache_invokes_the_registration_class_beside_a_generic_component_without_a_sheet()
    {
        var calls = __NazorStyles_HotGeneric.Calls;
        StyleHotReload.ClearCache([typeof(HotGeneric<>)]);
        Assert.Equal(calls + 1, __NazorStyles_HotGeneric.Calls);
        Assert.Null(StyleHotReload.LastError);
    }

    [Fact]
    public void ClearCache_keeps_a_failing_factory_in_LastError_and_does_not_throw()
    {
        var fail = false;
        StyleRegistry.RegisterScoped(typeof(HotEpsilon), () => fail ? throw new InvalidOperationException("boom") : Sheet(".e { padding: 1px; }"));
        var created = StyleRegistry.GetScoped(typeof(HotEpsilon))!;

        fail = true;
        StyleHotReload.ClearCache([typeof(HotEpsilon)]);
        var error = Assert.IsType<InvalidOperationException>(StyleHotReload.LastError);
        Assert.Equal("boom", error.Message);
        Assert.Same(created, StyleRegistry.GetScoped(typeof(HotEpsilon)));

        fail = false;
        StyleHotReload.ClearCache(null);
        Assert.Null(StyleHotReload.LastError);
    }
}
