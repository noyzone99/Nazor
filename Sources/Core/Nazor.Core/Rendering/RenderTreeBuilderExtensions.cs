namespace Nazor.Rendering;

/// <summary>The ABI of Razor's event modifier emission.</summary>
public static class WebRenderTreeBuilderExtensions
{
    /// <summary>Sets whether the event's default behavior should be prevented.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="eventName">The normalized event attribute name.</param>
    /// <param name="value">The value to use.</param>
    public static void AddEventPreventDefaultAttribute(RenderTreeBuilder builder, int sequence, string eventName, bool value) =>
        builder.AddEventPreventDefaultAttribute(sequence, eventName, value);

    /// <summary>Sets whether the event should stop propagating to ancestor elements.</summary>
    /// <param name="builder">The builder that receives the render frames.</param>
    /// <param name="sequence">The source-order sequence number used to match frames across renders.</param>
    /// <param name="eventName">The normalized event attribute name.</param>
    /// <param name="value">The value to use.</param>
    public static void AddEventStopPropagationAttribute(RenderTreeBuilder builder, int sequence, string eventName, bool value) =>
        builder.AddEventStopPropagationAttribute(sequence, eventName, value);
}
