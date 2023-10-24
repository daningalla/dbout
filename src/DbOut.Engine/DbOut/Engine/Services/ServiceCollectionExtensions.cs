using DbOut.Engine.Pipeline;
using DbOut.Options;
using DbOut.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vertical.Pipelines.DependencyInjection;

namespace DbOut.Engine.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDbOutEngine(
        this IServiceCollection services,
        Action<DatabaseProviderBuilder> providerBuilder)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(providerBuilder);
        
        services.AddServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        services.ConfigurePipeline<JobContext>(pipeline => pipeline
                .UseMiddleware<CleanOutputDirectoryTask>()
                .UseMiddleware<ListProvidersTask>()
                .UseMiddleware<ValidateDatabaseProviderTask>()
                .UseMiddleware<ValidateConnectionOptions>()
                .UseMiddleware<QuerySchemaTask>()
                .UseMiddleware<ValidateWatermarkColumnTask>()
                .UseMiddleware<PrepareFileSystemTask>()
                .UseMiddleware<InitializeRestorePointTask>()
                .UseMiddleware<InitializeTelemetryTask>()
                .UseMiddleware<SaveRuntimeOptionsTask>()
                .UseMiddleware<InitializeQueryEngineTask>()
                .UseMiddleware<FlushOutputAggregatorTask>()
                .UseMiddleware<SaveTelemetryTask>(),
            ServiceLifetime.Singleton);
        
        providerBuilder(new DatabaseProviderBuilder(services));
        
        return services;
    }

    public static IServiceCollection AddRuntimeOptions(
        this IServiceCollection services,
        IOptions<RuntimeOptions> options)
    {
        return services.AddSingleton(options);
    }
}