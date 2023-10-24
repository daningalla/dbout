using DbOut.Exceptions;
using DbOut.Options;
using DbOut.Providers;
using DbOut.Services;
using Microsoft.Extensions.Logging;
using Vertical.Pipelines;

namespace DbOut.Engine.Pipeline;

public class ValidateDatabaseProviderTask : CommandTask
{
    private readonly ILogger<ValidateDatabaseProviderTask> _logger;
    private readonly IRuntimeServices _runtimeServices;

    public ValidateDatabaseProviderTask(
        ILogger<ValidateDatabaseProviderTask> logger,
        IRuntimeServices runtimeServices)
        : base(logger, new[] { CommandMode.Execute, CommandMode.GetSchema })
    {
        _logger = logger;
        _runtimeServices = runtimeServices;
    }

    protected override Task InvokeCoreAsync(JobContext context, CancellationToken _)
    {
        var provider = _runtimeServices.DatabaseProvider;

        _logger.LogInformation("Validated database provider {provider}", provider.ProviderId);

        return Task.CompletedTask;
    }
}