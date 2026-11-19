namespace Nazor.Compiler.Tests;

public sealed class DBusBindingsTests
{
    [Fact]
    public void Checked_in_static_bindings_match_all_desktop_contracts()
    {
        var directory = Path.Combine(Nazor.Testing.RepositoryPaths.Root, "Sources", "Platforms", "Nazor.Platform.Linux");
        var generated = DBusBindings.Generate(directory, typeof(Tmds.DBus.Protocol.ObjectPath).Assembly.Location);
        Assert.Equal(File.ReadAllText(Path.Combine(directory, "DesktopBindings.g.cs")).ReplaceLineEndings("\n"), generated.ReplaceLineEndings("\n"));
    }
}
