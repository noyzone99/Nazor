using System.Runtime.InteropServices;
using Nazor.Platform.Linux;

namespace Nazor.Linux.Tests;

public sealed class DrmNativeTests
{
    [Fact]
    public void Drm_structures_match_the_64_bit_linux_ABI()
    {
        Assert.Equal(8, IntPtr.Size);
        Assert.Equal(68, Marshal.SizeOf<DrmNative.Mode>());
        Assert.Equal(80, Marshal.SizeOf<DrmNative.Resources>());
        Assert.Equal(88, Marshal.SizeOf<DrmNative.Connector>());
        Assert.Equal(20, Marshal.SizeOf<DrmNative.Encoder>());
        Assert.Equal(100, Marshal.SizeOf<DrmNative.Crtc>());
        Assert.Equal(8, Marshal.SizeOf<DrmNative.PollFd>());
        Assert.Equal(40, Marshal.SizeOf<DrmNative.EventContext>());
        Assert.Equal(32, Marshal.SizeOf<DrmNative.CreateDumb>());
        Assert.Equal(16, Marshal.SizeOf<DrmNative.MapDumb>());
        Assert.Equal(40, Marshal.OffsetOf<DrmNative.Connector>("Modes").ToInt32());
        Assert.Equal(28, Marshal.OffsetOf<DrmNative.Crtc>("Mode").ToInt32());
    }
}
