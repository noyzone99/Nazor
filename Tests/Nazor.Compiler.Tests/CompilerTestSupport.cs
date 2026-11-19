using System.Reflection;
using Nazor.Compiler;

namespace Nazor.Compiler.Tests;

internal static class Build
{
    public static readonly string Core = typeof(ComponentBase).Assembly.Location;
    public static readonly string Controls = typeof(Nazor.Controls.Button).Assembly.Location;
    public static readonly string Style = typeof(Nazor.Style.StyleSheet).Assembly.Location;
    public static readonly string Library = Path.Combine(AppContext.BaseDirectory, "Nazor.Sample.Components.dll");

    public static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    public static string Nazor(string name) => Fixture(name + ".nazor");

    public static NazorCompiler Compiler(params string[] references) => new(new NazorCompilerOptions { References = [Core, Controls, Style, .. references] });

    public static Assembly Load(this NazorCompiler compiler, IReadOnlyList<GeneratedComponent> components, params string[] sources) =>
        Assembly.Load(compiler.Emit(components, "Nazor.Tests." + Guid.NewGuid().ToString("N"), sources));

    public static Type ComponentType(this Assembly assembly, string name) => assembly.GetTypes().Single(t => t.Name == name);

    public static NazorCompilationException Fails(Action action) => Assert.Throws<NazorCompilationException>(action);
}

public sealed class CompilerFixture
{
    public NazorCompiler Compiler { get; } = Build.Compiler();
}
