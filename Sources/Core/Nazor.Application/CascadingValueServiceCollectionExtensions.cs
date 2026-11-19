using Nazor;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Root cascades are created in the window's DI scope and used by its renderer.</summary>
public static class CascadingValueServiceCollectionExtensions
{
    /// <summary>Registers a root cascading value or value source with the dependency injection container.</summary>
    /// <param name="services">The dependency injection service collection or provider used by the application.</param>
    /// <param name="valueFactory">The factory that resolves the root cascading value from application services.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The resulting IServiceCollection value.</returns>
    /// <exception cref="ArgumentNullException">valueFactory is null.</exception>
    public static IServiceCollection AddCascadingValue<TValue>(this IServiceCollection services, Func<IServiceProvider, TValue> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);
        return services.AddCascadingValue<TValue>(provider => new CascadingValueSource<TValue>(valueFactory(provider), isFixed: true));
    }

    /// <summary>Registers a root cascading value or value source with the dependency injection container.</summary>
    /// <param name="services">The dependency injection service collection or provider used by the application.</param>
    /// <param name="name">The name used to identify the parameter, property, resource, or registration.</param>
    /// <param name="valueFactory">The factory that resolves the root cascading value from application services.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The resulting IServiceCollection value.</returns>
    /// <exception cref="ArgumentNullException">valueFactory is null.</exception>
    public static IServiceCollection AddCascadingValue<TValue>(this IServiceCollection services, string name, Func<IServiceProvider, TValue> valueFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(valueFactory);
        return services.AddCascadingValue<TValue>(provider => new CascadingValueSource<TValue>(name, valueFactory(provider), isFixed: true));
    }

    /// <summary>Registers a root cascading value or value source with the dependency injection container.</summary>
    /// <param name="services">The dependency injection service collection or provider used by the application.</param>
    /// <param name="sourceFactory">The factory that creates the root cascading value source.</param>
    /// <typeparam name="TValue">The type of the component, callback, or cascading value.</typeparam>
    /// <returns>The resulting IServiceCollection value.</returns>
    /// <exception cref="ArgumentNullException">services, sourceFactory is null.</exception>
    public static IServiceCollection AddCascadingValue<TValue>(this IServiceCollection services, Func<IServiceProvider, CascadingValueSource<TValue>> sourceFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sourceFactory);
        services.AddScoped<ICascadingValueSupplier>(provider => sourceFactory(provider));
        return services;
    }
}
