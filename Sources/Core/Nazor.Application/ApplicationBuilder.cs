using System.Globalization;

namespace Nazor;

/// <summary>Configures application startup, culture overrides and shutdown through the owning host builder.</summary>
public sealed class ApplicationBuilder
{
    private readonly NazorApplicationBuilder owner;

    internal ApplicationBuilder(NazorApplicationBuilder owner) => this.owner = owner;

    internal ApplicationConfiguration Configuration { get; } = new();

    /// <summary>Initial formatting culture override; null preserves CurrentCulture at startup.</summary>
    public CultureInfo? Culture
    {
        get => Configuration.Culture;
        set => Configuration.Culture = value;
    }

    /// <summary>Initial resource language override; null preserves CurrentUICulture at startup.</summary>
    public CultureInfo? UICulture
    {
        get => Configuration.UICulture;
        set => Configuration.UICulture = value;
    }

    /// <summary>Determines when closing windows ends the application.</summary>
    public ShutdownMode ShutdownMode
    {
        get => Configuration.ShutdownMode;
        set => Configuration.ShutdownMode = value;
    }

    /// <summary>Selects a component that declares <c>@window</c>; native creation is deferred to Run.</summary>
    /// <typeparam name="TComponent">The component declaring the main window.</typeparam>
    /// <param name="opening">Optional activation and positioning context.</param>
    public void UseMainWindow<TComponent>(WindowOpenOptions? opening = null) where TComponent : IComponent =>
        owner.UseMainWindow<TComponent>(opening);

    /// <summary>Declares a main window for content without <c>@window</c>; native creation is deferred to Run.</summary>
    /// <typeparam name="TComponent">The content component.</typeparam>
    /// <param name="configure">Configures this window only; values are copied at Build.</param>
    public void CreateMainWindow<TComponent>(Action<WindowConfiguration>? configure = null) where TComponent : IComponent =>
        owner.CreateMainWindow<TComponent>(configure);

    /// <summary>Selects content for an ISingleViewPlatform surface without desktop windows.</summary>
    /// <typeparam name="TComponent">The root view component, without <c>@window</c>.</typeparam>
    public void UseMainView<TComponent>() where TComponent : IComponent => owner.UseMainView<TComponent>();

    /// <summary>Registers one independently scoped tray item of this type, created during Run.</summary>
    public void UseTrayItem<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class, ITrayItem => owner.UseTrayItem<T>();
}
