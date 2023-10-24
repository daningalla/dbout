using DbOut.IO;
using DbOut.Options;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class FlushOutputAggregatorTask : CommandTask
{
    private readonly IOutputAggregator _outputAggregator;

    public FlushOutputAggregatorTask(
        ILogger<FlushOutputAggregatorTask> logger,
        IOutputAggregator outputAggregator) : 
        base(logger, new[] { CommandMode.Execute })
    {
        _outputAggregator = outputAggregator;
    }

    /// <inheritdoc />
    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        await _outputAggregator.FlushAsync(cancellationToken);
        Logger.LogInformation("Flushed any remaining cache files.");
    }
}