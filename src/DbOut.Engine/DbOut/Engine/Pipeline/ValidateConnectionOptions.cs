using DbOut.Options;
using DbOut.Providers;
using DbOut.Services;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class ValidateConnectionOptions : CommandTask
{
    private readonly IRuntimeServices _runtimeServices;

    public ValidateConnectionOptions(
        ILogger<ValidateConnectionOptions> logger,
        IRuntimeServices runtimeServices) 
        : base(logger, new[] { CommandMode.Execute, CommandMode.ValidateConnection, CommandMode.GetSchema })
    {
        _runtimeServices = runtimeServices;
    }

    /// <inheritdoc />
    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        var connectionContext = _runtimeServices.ConnectionContext;

        await connectionContext.HealthCheckAsync(cancellationToken);
        
        Logger.LogInformation("Connection health check complete.");
    }
}