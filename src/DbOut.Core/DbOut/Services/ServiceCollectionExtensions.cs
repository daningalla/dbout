using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DbOut.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services) =>
        services.AddServices(typeof(ServiceCollectionExtensions).Assembly);
    
    public static IServiceCollection AddServices(this IServiceCollection services, Assembly assembly)
    {
        var entries = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Select(type => new
            {
                type,
                registration = type.GetCustomAttribute<ServiceRegistrationAttribute>()
            })
            .Where(entry => entry.registration != null);

        foreach (var entry in entries)
        {
            TryRegisterService(services, entry.type, entry.registration!.Lifetime);
        }
        
        return services;
    }

    private static void TryRegisterService(
        IServiceCollection services, 
        Type implementationType, 
        ServiceLifetime lifetime)
    {
        var serviceTypes = implementationType
            .GetInterfaces()
            .Where(serviceType => serviceType != typeof(IDisposable) && serviceType != typeof(IAsyncDisposable))
            .ToArray();

        switch (serviceTypes.Length)
        {
            case 0:
                services.Add(ServiceDescriptor.Describe(implementationType, implementationType, lifetime));
                break;
            
            case 1:
                services.Add(ServiceDescriptor.Describe(serviceTypes[0], implementationType, lifetime));
                break;
            
            default:
                services.Add(ServiceDescriptor.Describe(implementationType, implementationType, lifetime));
                foreach (var serviceType in serviceTypes)
                {
                    services.Add(ServiceDescriptor.Describe(
                        serviceType,
                        serviceProvider => serviceProvider.GetRequiredService(implementationType),
                        lifetime));
                }
                break;
        }
    }
}