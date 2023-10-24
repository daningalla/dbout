using DbOut.Options;
using DbOut.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbOut.Engine.Pipeline;

public class ListProvidersTask : CommandTask
{
    private readonly IOptions<RuntimeOptions> _options;
    private readonly IEnumerable<IDatabaseProvider> _providers;

    /// <summary>
    /// Creates a new instance of this type
    /// </summary>
    public ListProvidersTask(
        IOptions<RuntimeOptions> options,
        ILogger<ListProvidersTask> logger,
        IEnumerable<IDatabaseProvider> providers)
        : base(logger, new[] { CommandMode.ListProviders })
    {
        _options = options;
        _providers = providers;
    }

    /// <inheritdoc />
    protected override Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        var providerOutput = string.Join(Environment.NewLine,
            _providers.Select((impl, index) => $"{index+1} - {impl.ProviderId}"));
        
        Logger.LogInformation("Configured database providers:\n{providers}", providerOutput);

        return Task.CompletedTask;
    }
}