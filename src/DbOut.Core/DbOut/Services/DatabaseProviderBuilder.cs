using Microsoft.Extensions.DependencyInjection;

namespace DbOut.Services;

public class DatabaseProviderBuilder
{
    public DatabaseProviderBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}