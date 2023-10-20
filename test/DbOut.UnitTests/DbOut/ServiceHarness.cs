using DbOut.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DbOut;

public abstract class ServiceHarness<T> : IDisposable where T : class
{
    public IServiceProvider ServiceProvider { get; }

    protected ServiceHarness(Action<IServiceCollection>? builder = null)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var collection = new ServiceCollection()
            .AddCoreServices()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging();
        builder?.Invoke(collection);
        ServiceProvider = collection.BuildServiceProvider();
        Instance = ServiceProvider.GetRequiredService<T>();
    }
    
    public TService GetService<TService>() where TService : class => ServiceProvider.GetRequiredService<TService>();
    
    public T Instance { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        (ServiceProvider as IDisposable)?.Dispose();
    }
}