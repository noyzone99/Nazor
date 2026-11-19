using System.Runtime.InteropServices;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Testing;
using static Nazor.Platform.Windows.Win32;

namespace Nazor.Application.Tests;

[Collection("Windows")]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class EventWindowTests
{
    private static EventForm Form(NazorWindow window) => (EventForm)window.Root.Component;
    private static Point ButtonCenter(NazorWindow window)
    {
        var bounds = window.Scene.GetBounds(Form(window).Button.Element.Element!);
        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    [Fact]
    public void Horizontal_wheel_messages_scroll_a_horizontal_viewer()
    {
        using var harness = new WindowHarness(typeof(Gallery));
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        static Rendering.ElementNode Strip(NazorWindow window) => window.Root.Elements("Container").Single(e => e.Id == "strip");
        var center = harness.Read(w =>
        {
            var bounds = w.Scene.GetBounds(Strip(w));
            return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        });
        harness.Wheel(center, 240, horizontal: true);
        harness.WaitUntil(w => w.Scene.GetScrollState(Strip(w))!.OffsetX > 0, "horizontal wheel");
        Assert.Equal(96, harness.Read(w => w.Scene.GetScrollState(Strip(w))!.OffsetX));

        harness.Wheel(center, -120);
        harness.Wheel(center, -120, horizontal: true);
        harness.WaitUntil(w => w.Scene.GetScrollState(Strip(w))!.OffsetX == 48, "second horizontal wheel");
        Assert.Equal(0, harness.Read(w => w.Scene.GetScrollState(Strip(w))!.OffsetY));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void Native_messages_deliver_keys_focus_double_click_context_and_capture()
    {
        using var harness = new WindowHarness(typeof(EventForm));
        harness.WaitUntil(w => w.FrameCount > 0, "first frame");
        var point = harness.Read(ButtonCenter);
        harness.Click(point);
        harness.Key(0x41, true);
        harness.Key(0x41, false);
        harness.Mouse(WM_LBUTTONDBLCLK, point, 1);
        harness.Mouse(WM_LBUTTONUP, point);
        harness.Mouse(WM_RBUTTONDOWN, point);
        harness.Mouse(WM_RBUTTONUP, point);
        harness.WaitUntil(w => Form(w).Log.Contains("button:context"), "native event delivery");
        var log = harness.Read(w => Form(w).Log.ToArray());
        Assert.Contains("button:keydown:a:None:False", log);
        Assert.Contains("button:keyup:a:None:False", log);
        Assert.Contains("button:focus", log);
        Assert.Contains("button:double", log);
        harness.Read(w => { Form(w).CaptureOnDown = true; return true; });
        harness.Mouse(WM_LBUTTONDOWN, point, 1);
        harness.WaitUntil(w => w.Input.CapturedPointer is not null, "pointer capture");
        harness.Mouse(WM_MOUSEMOVE, new Point(-20, -20));
        harness.Mouse(WM_LBUTTONUP, new Point(-20, -20));
        harness.WaitUntil(w => w.Input.CapturedPointer is null, "capture release");
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void PreventDefault_suppresses_native_character_insertion()
    {
        using var harness = new WindowHarness(typeof(EventForm));
        harness.Read(w =>
        {
            var form = Form(w);
            w.Input.Focus(form.Box.InputElement.Element!, false);
            var box = form.Box;
            box.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["AdditionalAttributes"] = new Dictionary<string, object?> { ["__internal_preventDefault_onkeydown"] = true },
            }));
            return true;
        });
        harness.Key(0x41, true);
        harness.Key(0x41, false);
        harness.Read(w => true);
        harness.Read(w => true);
        Assert.Equal("", harness.Read(w => Form(w).Text));
        Assert.Equal("", harness.Read(w => w.Input.FocusedTextBox!.GetFeature<Nazor.Presentation.TextEditState>()!.Value));
        Assert.Empty(harness.Read(w => w.Exceptions));
    }

    [Fact]
    public void SendInput_shortcut_has_priority_over_focused_textbox()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("NAZOR_SENDINPUT_TESTS") == "1",
            "Set NAZOR_SENDINPUT_TESTS=1 on an interactive desktop: this test temporarily takes the foreground.");
        using var harness = new WindowHarness(typeof(EventForm));
        var previous = NativeInput.GetForegroundWindow();
        try
        {
            harness.Read(w =>
            {
                w.Show(activate: true);
                w.Input.Focus(Form(w).Box.InputElement.Element!, false);
                w.Input.KeyBindings.Register("n", KeyModifiers.Control,
                    EventCallback.Factory.Create<KeyboardEventArgs>(Form(w), () => Form(w).Log.Add("shortcut")));
                return true;
            });
            NativeInput.SetForegroundWindow(harness.Handle);
            Assert.True(SpinWait.SpinUntil(() => NativeInput.GetForegroundWindow() == harness.Handle, TimeSpan.FromSeconds(3)), "The SendInput window must be foreground.");
            NativeInput.SendKeys((0x11, false), (0x4E, false), (0x4E, true), (0x11, true));
            harness.WaitUntil(w => Form(w).Log.Contains("shortcut"), "SendInput Ctrl+N");
            Assert.DoesNotContain(harness.Read(w => Form(w).Log), e => e.StartsWith("box:keydown:n", StringComparison.Ordinal));
            Assert.Equal("", harness.Read(w => Form(w).Text));
            Assert.Empty(harness.Read(w => w.Exceptions));
        }
        finally
        {
            NativeInput.SendKeysUnchecked((0x4E, true), (0x11, true));
            if (previous != 0) NativeInput.SetForegroundWindow(previous);
        }
    }

    private static class NativeInput
    {
        [StructLayout(LayoutKind.Explicit, Size = 40)]
        private struct Input
        {
            [FieldOffset(0)] public uint Type;
            [FieldOffset(8)] public ushort VirtualKey;
            [FieldOffset(12)] public uint Flags;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, [In] Input[] inputs, int size);
        [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetForegroundWindow(nint window);

        public static void SendKeys(params (ushort Key, bool Up)[] keys)
        {
            var sent = SendKeysUnchecked(keys);
            Assert.True(sent == keys.Length, $"SendInput delivered {sent}/{keys.Length} events (Win32 error {Marshal.GetLastWin32Error()}).");
        }

        public static uint SendKeysUnchecked(params (ushort Key, bool Up)[] keys)
        {
            var inputs = keys.Select(k => new Input { Type = 1, VirtualKey = k.Key, Flags = k.Up ? 2u : 0 }).ToArray();
            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        }
    }
}
