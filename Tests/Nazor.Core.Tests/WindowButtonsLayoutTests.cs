namespace Nazor.Core.Tests;

public sealed class WindowButtonsLayoutTests
{
    [Fact]
    public void Layout_is_an_immutable_snapshot_with_value_equality()
    {
        WindowButton[] left = [WindowButton.Close];
        WindowButton[] right = [WindowButton.Minimize, WindowButton.Maximize];
        var layout = new WindowButtonsLayout(left, right);
        var same = new WindowButtonsLayout([WindowButton.Close], [WindowButton.Minimize, WindowButton.Maximize]);
        left[0] = WindowButton.Minimize;
        right[0] = WindowButton.Close;

        Assert.Equal(same, layout);
        Assert.Equal(same.GetHashCode(), layout.GetHashCode());
        Assert.Throws<NotSupportedException>(() => ((IList<WindowButton>)layout.Left)[0] = WindowButton.Maximize);
        Assert.NotEqual(layout, new WindowButtonsLayout([WindowButton.Close], [WindowButton.Maximize, WindowButton.Minimize]));
        Assert.NotEqual(layout, new WindowButtonsLayout([], [WindowButton.Close, WindowButton.Minimize, WindowButton.Maximize]));
        Assert.Empty(WindowButtonsLayout.Empty.Left);
        Assert.Empty(WindowButtonsLayout.Empty.Right);
        Assert.Empty(WindowButtonsLayout.Default.Left);
        Assert.Equal(new[] { WindowButton.Minimize, WindowButton.Maximize, WindowButton.Close }, WindowButtonsLayout.Default.Right);
    }
}
