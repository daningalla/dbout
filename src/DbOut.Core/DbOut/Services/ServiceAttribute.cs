using Microsoft.Extensions.DependencyInjection;

namespace DbOut.Services;

/// <summary>
/// Decorates a class as a service.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="lifetime">Service lifetime, defaults to singleton.</param>
    public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Gets the lifetime.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Lifetime}";
}