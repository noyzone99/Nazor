using Microsoft.Extensions.DependencyInjection;
using Nazor.Graphics;
using Nazor.Platform;
using SkiaSharp;

namespace Nazor.Application.Tests;

public sealed class ApplicationLauncherTests
{
    private sealed class Native : IPlatformDesktopIntegration
    {
        public ApplicationLauncherCapabilities LauncherCapabilities { get; set; } = new(true, true, true);
        public event Action? LauncherCapabilitiesChanged;
        public event Action? NotificationActivated { add { } remove { } }
        public bool Accept { get; set; } = true;
        public IReadOnlyList<JumpListTask> Tasks { get; private set; } = [];
        public int? Count { get; private set; }
        public IReadOnlyList<RenderedImage>? Images { get; private set; }
        public string? Description { get; private set; }
        public bool SetJumpList(IReadOnlyList<JumpListTask> tasks) { if (!Accept) return false; Tasks = tasks; return true; }
        public bool SetBadgeCount(int count) { if (!Accept) return false; Count = count; Images = null; return true; }
        public bool SetBadgeIcon(IReadOnlyList<RenderedImage> images, string description)
        { if (!Accept) return false; Images = images; Description = description; Count = null; return true; }
        public bool ClearBadge() { if (!Accept) return false; Count = null; Images = null; return true; }
        public void ChangeCapabilities(ApplicationLauncherCapabilities value) { LauncherCapabilities = value; LauncherCapabilitiesChanged?.Invoke(); }
        public bool ShowNotification(SystemNotification notification) => false;
        public void WithdrawNotification() { }
    }

    [Fact]
    public void Launcher_is_shared_across_scopes_and_only_operates_during_Run_on_the_UI_thread()
    {
        using var services = new ServiceCollection().AddNazorServices().BuildServiceProvider();
        using var scope = services.CreateScope();
        var desktop = services.GetRequiredService<DesktopServices>();
        var launcher = services.GetRequiredService<IApplicationLauncher>();
        Assert.Same(launcher, scope.ServiceProvider.GetRequiredService<IApplicationLauncher>());
        Assert.Same(desktop.Launcher, launcher);
        Assert.Equal(default, launcher.Capabilities);
        Assert.Throws<InvalidOperationException>(() => launcher.SetBadgeCount(1));
        var dispatcher = new UIDispatcher();
        var native = new Native();
        desktop.Start(native, dispatcher);
        Assert.True(launcher.SetBadgeCount(2));
        Exception? error = null;
        var worker = new Thread(() => error = Record.Exception(() => launcher.ClearBadge()));
        worker.Start();
        worker.Join();
        Assert.IsType<InvalidOperationException>(error);
        desktop.Stop();
        Assert.Null(native.Count);
        Assert.Equal(default, launcher.Capabilities);
        Assert.Throws<InvalidOperationException>(() => launcher.SetJumpList([]));
        Assert.Throws<InvalidOperationException>(() => launcher.ClearBadge());
    }

    [Fact]
    public void Task_snapshots_preserve_individual_arguments_and_validate_before_publication()
    {
        var launcher = new ApplicationLauncher();
        var native = new Native();
        launcher.Start(native, new UIDispatcher());
        var arguments = new[] { "", "a b", "quote\"", "C:\\folder\\", "日本語" };
        var tasks = new[] { new JumpListTask("Open", arguments) };
        Assert.True(launcher.SetJumpList(tasks));
        arguments[0] = "changed";
        tasks[0] = new("Other", []);
        Assert.Equal("Open", native.Tasks[0].Title);
        Assert.Equal("", native.Tasks[0].Arguments[0]);
        var snapshot = native.Tasks;
        Assert.Throws<ArgumentException>(() => launcher.SetJumpList([new(" ", [])]));
        Assert.Throws<ArgumentException>(() => launcher.SetJumpList([new("NUL\0", [])]));
        Assert.Throws<ArgumentException>(() => launcher.SetJumpList([new("Task", ["a\0b"])]));
        Assert.Throws<ArgumentException>(() => launcher.SetJumpList([new("Task", [null!])]));
        Assert.Throws<ArgumentNullException>(() => launcher.SetJumpList([new("Task", null!)]));
        Assert.Same(snapshot, native.Tasks);
        Assert.True(launcher.SetJumpList([]));
        Assert.Empty(native.Tasks);
        launcher.Stop();
    }

    [Fact]
    public void Capability_changes_are_dispatched_and_rejected_requests_preserve_the_previous_badge()
    {
        var dispatcher = new UIDispatcher();
        var native = new Native();
        var launcher = new ApplicationLauncher();
        launcher.Start(native, dispatcher);
        var changed = 0;
        launcher.CapabilitiesChanged += () => { Assert.True(dispatcher.CheckAccess()); changed++; };
        Assert.True(launcher.SetBadgeCount(3));
        native.Accept = false;
        Assert.False(launcher.SetBadgeCount(7));
        Assert.False(launcher.ClearBadge());
        Assert.Equal(3, native.Count);
        native.ChangeCapabilities(default);
        Assert.Equal(0, changed);
        dispatcher.RunPending();
        Assert.Equal(1, changed);
        Assert.False(launcher.SetJumpList([new("Open", [])]));
        Assert.False(launcher.SetBadgeCount(1));
        Assert.False(launcher.SetBadgeIcon(ImageSource.FromBytes([]), "Unavailable"));
        Assert.Throws<ArgumentOutOfRangeException>(() => launcher.SetBadgeCount(-1));
        Assert.Throws<ArgumentException>(() => launcher.SetBadgeIcon(ImageSource.FromBytes([]), "\0"));
        native.Accept = true;
        native.ChangeCapabilities(new(false, false, true));
        Assert.True(launcher.SetBadgeCount(0));
        Assert.Null(native.Count);
        native.ChangeCapabilities(default);
        launcher.Stop();
        dispatcher.RunPending();
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Icon_variants_replace_counts_and_later_counts_or_clear_cancel_stale_image_loads()
    {
        var native = new Native();
        var dispatcher = new UIDispatcher();
        var launcher = new ApplicationLauncher();
        launcher.Start(native, dispatcher);
        var bytes = ImageBytes();
        Assert.True(launcher.SetBadgeCount(4));
        Assert.True(launcher.SetBadgeIcon(ImageSource.FromBytes(bytes), "Unread messages"));
        Assert.True(dispatcher.RunUntil(() => native.Images is not null, TimeSpan.FromSeconds(5)));
        Assert.Null(native.Count);
        Assert.Equal("Unread messages", native.Description);
        Assert.Equal(new[] { 16, 20, 24, 32, 48, 64 }, native.Images!.Select(image => image.Width));
        Assert.Equal(new[] { 8, 10, 12, 16, 24, 32 }, native.Images!.Select(image => image.Height));
        foreach (var clear in new[] { false, true })
        {
            var pending = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
            Assert.True(launcher.SetBadgeIcon(ImageSource.FromStream(_ => pending.Task, Guid.NewGuid().ToString()), "Delayed"));
            Assert.True(clear ? launcher.ClearBadge() : launcher.SetBadgeCount(8));
            var stream = new MemoryStream(bytes);
            pending.SetResult(stream);
            Assert.True(dispatcher.RunUntil(() => !stream.CanRead, TimeSpan.FromSeconds(5)));
            dispatcher.RunPending();
            Assert.Null(native.Images);
            Assert.Equal(clear ? null : (int?)8, native.Count);
        }
        var stopped = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.True(launcher.SetBadgeIcon(ImageSource.FromStream(_ => stopped.Task, "stopped-badge"), "Stopped"));
        launcher.Stop();
        var stoppedStream = new MemoryStream(bytes);
        stopped.SetResult(stoppedStream);
        Assert.True(dispatcher.RunUntil(() => !stoppedStream.CanRead, TimeSpan.FromSeconds(5)));
        dispatcher.RunPending();
        Assert.Null(native.Images);
        Assert.Null(native.Count);
    }

    [Fact]
    public void Rejected_count_does_not_cancel_an_accepted_pending_icon()
    {
        var native = new Native();
        var dispatcher = new UIDispatcher();
        var launcher = new ApplicationLauncher();
        launcher.Start(native, dispatcher);
        var pending = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.True(launcher.SetBadgeIcon(ImageSource.FromStream(_ => pending.Task, "pending-badge"), "Pending"));
        native.Accept = false;
        Assert.False(launcher.SetBadgeCount(9));
        native.Accept = true;
        pending.SetResult(new MemoryStream(ImageBytes()));
        Assert.True(dispatcher.RunUntil(() => native.Images is not null, TimeSpan.FromSeconds(5)));
        Assert.Equal("Pending", native.Description);
        launcher.Stop();
    }

    private static byte[] ImageBytes()
    {
        using var bitmap = new SKBitmap(8, 4);
        bitmap.Erase(SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
