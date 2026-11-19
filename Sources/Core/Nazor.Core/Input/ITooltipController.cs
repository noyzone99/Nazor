using Nazor.Rendering;

namespace Nazor.Input;

/// <summary>Connects composable tooltip content to the window's shared hover/focus scheduler.</summary>
public interface ITooltipController
{
    /// <summary>Gets the optional opening delay; null uses the input manager's default.</summary>
    TimeSpan? Delay { get; }
    /// <summary>Gets whether this tooltip can currently be shown.</summary>
    bool IsAvailable { get; }
    /// <summary>Gets the retained tooltip surface for hover continuity.</summary>
    ElementNode? ContentElement { get; }
    /// <summary>Requests opening through the component's normal state contract.</summary>
    Task OpenAsync();
    /// <summary>Requests closing through the component's normal state contract.</summary>
    Task CloseAsync();
}
