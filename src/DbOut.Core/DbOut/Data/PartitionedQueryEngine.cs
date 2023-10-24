using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using DbOut.Services;
using Microsoft.Extensions.Logging;

namespace DbOut.Data;

[Service]
public class PartitionedQueryEngine : IPartitionedQueryEngine
{
    private readonly ILogger<PartitionedQueryEngine> _logger;
    private readonly IPartitionedQueryHandler _partitionedQueryHandler;

    public PartitionedQueryEngine(
        ILogger<PartitionedQueryEngine> logger,
        IPartitionedQueryHandler partitionedQueryHandler)
    {
        _logger = logger;
        _partitionedQueryHandler = partitionedQueryHandler;
    }

    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
    public async Task<QueryEngineExitState> ExecuteAsync(
        BatchingParameters parameters,
        CancellationToken cancellationToken)
    {
        using var monitor = new PartitionedQueryMonitor(_logger, outerCancellationToken: cancellationToken);
        var threadCount = parameters.Parallelization.MaxThreadCount;
        var channel = Channel.CreateBounded<PartitionedQuery>(new BoundedChannelOptions(threadCount)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = threadCount == 1,
            SingleWriter = true
        });
        var threads = Enumerable
            .Range(0, threadCount)
            .Select(_ => Task.Run(async () => await ReadQueryPartitionChannelAsync(channel.Reader, monitor)))
            .ToArray();
        
        _logger.LogInformation("Created {count} query thread(s)", threads.Length);
        _logger.LogInformation("Partitioned queries will be enqueued until {maxCount} records is reached or query result count is less than {batchSize}.",
            parameters.MaxRowCount,
            parameters.Parallelization.BatchSize);
        
        try
        {
            await EnqueueQueryPartitionChannelAsync(parameters, channel.Writer, monitor);
        }
        catch (OperationCanceledException)
        {
            monitor.ThrowIfFaulted();
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Cancellation requested from external source.");
                return QueryEngineExitState.ExternallyCancelled;
            }
        }

        _logger.LogDebug("Waiting for reader threads to complete.");
        await Task.WhenAll(threads);

        return QueryEngineExitState.Graceful;
    }

    private async Task EnqueueQueryPartitionChannelAsync(
        BatchingParameters parameters,
        ChannelWriter<PartitionedQuery> channelWriter,
        PartitionedQueryMonitor monitor)
    {
        var offset = parameters.RecordOffset;
        var recordsetBufferPool = new RecordsetBufferPool(
            parameters.ColumnSchema.Count,
            parameters.Parallelization.BatchSize,
            parameters.Parallelization.MaxThreadCount);
        var batchSize = parameters.Parallelization.BatchSize;
        
        while (!monitor.IsCancellationRequested && offset < parameters.MaxRowCount)
        {
            var query = new PartitionedQuery
            {
                ColumnSchema = parameters.ColumnSchema,
                WatermarkColumnMetadata = parameters.WatermarkColumnMetadata,
                RecordsetBufferPool = recordsetBufferPool,
                Offset = offset,
                BatchSize = batchSize
            };

            await channelWriter.WriteAsync(query, monitor.GlobalCancellationToken);

            offset += batchSize;
        }
        
        _logger.LogDebug("Completing query channel writer.");
        channelWriter.Complete();
    }

    private async Task ReadQueryPartitionChannelAsync(
        ChannelReader<PartitionedQuery> channelReader,
        PartitionedQueryMonitor monitor)
    {
        using var _ = _logger.BeginScope("Thread {id}", Environment.CurrentManagedThreadId);
        
        _logger.LogDebug("Starting partitioned query thread");
        
        await foreach (var query in channelReader.ReadAllAsync())
        {
            if (await HandlePartitionedQueryAsync(query, monitor) == 0)
                break;
        }
        
        _logger.LogDebug("Exiting partitioned query thread");
        monitor.SignalComplete();
    }

    private async Task<int> HandlePartitionedQueryAsync(
        PartitionedQuery query,
        PartitionedQueryMonitor monitor)
    {
        using var _ = _logger.BeginScope("PQuery= {id}", query.QueryId.ToString()[^8..]);
        
        try
        {
            return await _partitionedQueryHandler.QueryAsync(query, monitor.OuterCancellationToken);
        }
        catch (Exception exception)
        {
            monitor.SignalFault(exception);
            throw;
        }
    }
}