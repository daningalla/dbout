using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DbOut.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core services.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>A reference to this instance</returns>
    public static IServiceCollection AddDbOutCore(this IServiceCollection services) =>
        services
            .AddServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly)
            .AddMemoryCache();
    
    /// <summary>
    /// Adds automatically registered services.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly</param>
    /// <returns>A reference to this instance</returns>
    public static IServiceCollection AddServicesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var entries = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Select(type => new { type, registration = type.GetCustomAttribute<ServiceAttribute>() })
            .Where(entry => entry.registration != null);

        foreach (var entry in entries)
        {
            services.AddAutoRegisteredService(entry.type, entry.registration!);
        }

        return services;
    }

    private static void AddAutoRegisteredService(
        this IServiceCollection services,
        Type implementationType,
        ServiceAttribute registrationAttribute)
    {
        var serviceTypes = implementationType
            .GetInterfaces()
            .Where(type => type != typeof(IDisposable) && type != typeof(IAsyncDisposable))
            .ToArray();

        var lifetime = registrationAttribute.Lifetime;

        switch (serviceTypes.Length)
        {
            case 0:
                // Register as self
                services.Add(ServiceDescriptor.Describe(implementationType, implementationType, lifetime));
                break;
            
            case 1:
                // Register as service/implementation
                services.Add(ServiceDescriptor.Describe(serviceTypes[0], implementationType, lifetime));
                break;
            
            default:
                // Polymorphic registration
                services.Add(ServiceDescriptor.Describe(implementationType, implementationType, lifetime));
                foreach (var serviceType in serviceTypes)
                {
                    services.Add(ServiceDescriptor.Describe(serviceType, serviceProvider =>
                        serviceProvider.GetRequiredService(implementationType),
                        lifetime));
                }
                break;
        }
    }
}