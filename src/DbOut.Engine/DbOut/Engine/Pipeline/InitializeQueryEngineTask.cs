using DbOut.Continuation;
using DbOut.Data;
using DbOut.Metadata;
using DbOut.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbOut.Engine.Pipeline;

public class InitializeQueryEngineTask : CommandTask
{
    private readonly IPartitionedQueryEngine _partitionedQueryEngine;
    private readonly IMemoryCache _memoryCache;
    private readonly IRestorePoint _restorePoint;

    public InitializeQueryEngineTask(
        ILogger<InitializeQueryEngineTask> logger,
        IPartitionedQueryEngine partitionedQueryEngine,
        IMemoryCache memoryCache,
        IRestorePoint restorePoint) 
        : base(logger, new[] { CommandMode.Execute })
    {
        _partitionedQueryEngine = partitionedQueryEngine;
        _memoryCache = memoryCache;
        _restorePoint = restorePoint;
    }

    /// <inheritdoc />
    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Initializing partitioned query engine");

        var datasource = RuntimeOptions.ThrowIfNullReference("Options.DataSource", context.Options.DataSource);
        var parallelization = RuntimeOptions.ThrowIfNullReference("Options.Parallelization", 
            context.Options.Parallelization);
        var waterMarkColumnName = RuntimeOptions.ThrowIfNullOrEmpty(
            "Options.DataSource.WatermarkColumnName",
            context.Options.DataSource?.WatermarkColumnName);
        var schema = _memoryCache.Get<ColumnSchema>(nameof(ColumnSchema)) ?? throw new InvalidOperationException();
        var waterMarkColumn = schema[waterMarkColumnName];

        var restoredOffset = await _restorePoint.GetCommittedOffsetAsync();
        var offset = restoredOffset ?? 0;

        if (restoredOffset.HasValue)
        {
            Logger.LogInformation("Setting initial offset {offset} from restore point.", offset);
        }
        
        if (restoredOffset >= datasource.MaxRows)
        {
           Logger.LogInformation("Max row count of {maxRows} has been reached.", datasource.MaxRows);
           return;
        }
        
        var batchingParameters = new BatchingParameters(
            schema,
            waterMarkColumn,
            offset,
            datasource.MaxRows,
            parallelization);

        var exitState = await _partitionedQueryEngine.ExecuteAsync(batchingParameters, cancellationToken);
        Logger.LogInformation("Query engine exited with state {state}", exitState);
    }
}