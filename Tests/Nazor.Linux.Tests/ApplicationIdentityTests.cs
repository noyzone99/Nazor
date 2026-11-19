using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

public sealed class ApplicationIdentityTests
{
    [Fact]
    public void Desktop_identity_uses_application_metadata_instead_of_the_assembly_name()
    {
        Assert.Equal("org.nazor.linuxtests", LinuxDesktopWindow.AppId);
        Assert.Equal("Nazor.Linux.Tests", System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name);
    }
}
