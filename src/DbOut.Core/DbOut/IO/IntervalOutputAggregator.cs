using System.Collections.Concurrent;
using DbOut.Continuation;
using DbOut.DataChannels;
using DbOut.Exceptions;
using DbOut.Metadata;
using DbOut.Options;
using DbOut.Reporting;
using DbOut.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbOut.IO;

[Service]
public class IntervalOutputAggregator : IOutputAggregator
{
    private readonly ILogger<IntervalOutputAggregator> _logger;
    private readonly IRestorePoint _restorePoint;
    private readonly IFileSystem _fileSystem;
    private readonly IDataChannelFactory _dataChannelFactory;
    private readonly ITelemetryListener _telemetryListener;
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, CacheFileDescriptor> _descriptorQueue = new();
    private readonly Timer _intervalTimer;
    private readonly SemaphoreSlim _singleSemaphore = new(1);
    private readonly CancellationTokenSource _finalizingSource = new();
    private readonly OutputOptions _outputOptions;
    private readonly FileSize _configuredFileSize;
    private readonly string _table;
    private readonly ConcurrentBag<Exception> _timerExceptions = new();
    
    private int _committedOffset;

    public IntervalOutputAggregator(
        ILogger<IntervalOutputAggregator> logger,
        IRestorePoint restorePoint,
        IFileSystem fileSystem,
        IDataChannelFactory dataChannelFactory,
        ITelemetryListener telemetryListener,
        IMemoryCache memoryCache,
        IOptions<RuntimeOptions> options)
    {
        _logger = logger;
        _restorePoint = restorePoint;
        _fileSystem = fileSystem;
        _dataChannelFactory = dataChannelFactory;
        _telemetryListener = telemetryListener;
        _memoryCache = memoryCache;
        _table = RuntimeOptions.ThrowIfNullOrEmpty("Options.DataSource.TableName", options.Value.DataSource?.TableName);

        var timerInterval = RuntimeOptions
            .ThrowIfNullReference("Options.Parallelization", options.Value.Parallelization)
            .OutputFlushInterval;

        var intervalMs = (int)timerInterval.TotalMilliseconds;
        _intervalTimer = new Timer(AsyncTimerCallback);
        _intervalTimer.Change(intervalMs, Timeout.Infinite);
        _outputOptions = RuntimeOptions.ThrowIfNullReference("Options.Output", options.Value.Output);
        _configuredFileSize = RuntimeOptions.ThrowIfNullReference("Options.Output.MaxFileSize", _outputOptions.MaxFileSize);
    }

    public async Task EnqueueCachedFileAsync(CacheFileDescriptor descriptor, bool isFromRestorePoint)
    {
        ThrowTimerExceptions();
        if (!isFromRestorePoint)
        {
            await _restorePoint.AddCacheFileDescriptorAsync(descriptor);
        }

        if (!_descriptorQueue.TryAdd(descriptor.Id, descriptor))
            throw new InvalidOperationException("Failed to enqueue file descriptor,");
        _logger.LogTrace("Cache file {path} enqueued to output aggregator", descriptor.FilePath);
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        // Signal stopping so timer task start new work since we'll do it here
        _finalizingSource.Cancel();
        
        // Stop the timer
        _intervalTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        // Throw exceptions
        ThrowTimerExceptions();
        
        // Wait for pending flush to complete
        await _singleSemaphore.WaitAsync(cancellationToken);
        
        // Flush all
        await TryFlushCacheDataAsync(finalizing: true, cancellationToken);
    }

    private async void AsyncTimerCallback(object? _)
    {
        if (_finalizingSource.IsCancellationRequested)
            return;
        
        var entered = await _singleSemaphore.WaitAsync(TimeSpan.Zero);
        if (!entered)
        {
            // Process next time
            return;
        }

        try
        {
            _logger.LogTrace("Starting output aggregation interval");
            await TryFlushCacheDataAsync(finalizing: false, CancellationToken.None);
        }
        finally
        {
            _singleSemaphore.Release();
            _logger.LogTrace("Finished output aggregation interval");
        }
    }

    private async Task TryFlushCacheDataAsync(bool finalizing, CancellationToken cancellationToken)
    {
        try
        {
            await TryFlushCacheDataInErrorHandlerAsync(finalizing, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error occurred during output aggregation.");
            _timerExceptions.Add(exception);
            _finalizingSource.Cancel();
            _intervalTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
    
    private async Task TryFlushCacheDataInErrorHandlerAsync(bool finalizing, CancellationToken _)
    {
        _logger.LogTrace("Attempting output aggregation (finalizing={finalizing})", finalizing);

        // Loop until queue is empty of file size threshold not reached
        while (true)
        {
            var orderedDescriptors = BuildOrderedDescriptorCollection();

            if (orderedDescriptors.Count == 0)
            {
                _logger.LogDebug("Cache file queue is empty.");
                return;
            }

            var totalCachedBytes = orderedDescriptors.Sum(dsc => dsc.SizeInBytes);
            var maxFileSizeBytes = _configuredFileSize.ComputedLength;

            if (!finalizing && totalCachedBytes < maxFileSizeBytes)
            {
                _logger.LogTrace(
                    "Cached data size {cacheBytes}B < {fileSize}B (not finalizing), aggregation will be skipped",
                    totalCachedBytes,
                    maxFileSizeBytes);
                return;
            }

            _logger.LogDebug("Beginning output aggregation using {count} cache files", orderedDescriptors.Count);

            await FlushCacheDataAsync(orderedDescriptors);
        }
    }

    [Flags]
    private enum DescriptorAction
    {
        Merge = 1,
        DeleteFile = 2,
        Dequeue = 4,
        SetRestore = 8,
        UnsetRestore = 16,
        Enqueue = 32
    }

    private static IEnumerable<CacheFileDescriptor> SelectDescriptors(
        IEnumerable<(DescriptorAction Action, CacheFileDescriptor Descriptor)> descriptorActions,
        DescriptorAction actions)
    {
        return descriptorActions
            .Where(e => e.Action.HasFlag(actions))
            .Select(e => e.Descriptor);
    }
    
    private async Task FlushCacheDataAsync(IReadOnlyCollection<CacheFileDescriptor> orderedDescriptors)
    {
        var maxFileSize = _configuredFileSize.ComputedLength;
        var availableStagingBytes = (long)maxFileSize;
        var descriptorActions = new List<(DescriptorAction Action, CacheFileDescriptor Descriptor)>(
            orderedDescriptors.Count * 2);
        
        _logger.LogDebug("Evaluating {count} cache file descriptors, max buffer size={length} bytes.", 
            orderedDescriptors.Count,
            availableStagingBytes);

        foreach (var descriptor in orderedDescriptors)
        {
            _logger.LogTrace("Evaluating cache file, remaining buffer length = {length} bytes.",
                availableStagingBytes);
            
            if (availableStagingBytes - descriptor.SizeInBytes > 0)
            {
                // Consume the whole data file
                descriptorActions.Add((DescriptorAction.Merge 
                                       | DescriptorAction.DeleteFile 
                                       | DescriptorAction.Dequeue  
                                       | DescriptorAction.UnsetRestore, 
                    descriptor));
                
                availableStagingBytes -= descriptor.SizeInBytes;
                
                _logger.LogTrace("Enqueuing cache file to output file (offset={offset}, rowCount={rowCount})",
                    descriptor.CommitOffset,
                    descriptor.ActualCount);
                
                continue;
            }
            
            // Try to consume subset
            var bytesPerRecord = descriptor.SizeInBytes / descriptor.ActualCount;

            if (bytesPerRecord > maxFileSize)
            {
                _logger.LogError(
                    "A single record in cache file {path} exceeds max output file size. " +
                    "Increase the max file size to a minimum of {bytes} bytes.",
                    descriptor.FilePath,
                    bytesPerRecord);
                throw new CoreStopException();
            }

            if (bytesPerRecord > availableStagingBytes)
            {
                // Can't fit a single record into remaining space - we'll flush out
                // the files we have
                break;
            }

            // Split descriptor offsets and flush
            var pickCount = (int)(availableStagingBytes / bytesPerRecord);
            var (merge, defer) = descriptor.Split(pickCount);
            
            _logger.LogTrace("Splitting cache file to offsets ({firstOffset}->{firstStop})->({secondOffset}->{secondStop})",
                merge.CommitOffset,
                merge.CommitOffset + merge.ActualCount-1,
                defer.CommitOffset,
                defer.CommitOffset + defer.ActualCount-1);

            descriptorActions.Add((DescriptorAction.Dequeue | DescriptorAction.UnsetRestore, descriptor));
            descriptorActions.Add((DescriptorAction.Merge, merge));
            descriptorActions.Add((DescriptorAction.Enqueue, defer));
            break;
        }
        
        await WriteOutputFileAsync(SelectDescriptors(descriptorActions, DescriptorAction.Merge).ToArray());
        
        _logger.LogTrace("Deleting consumed cache files");
        DeleteConsumedCacheFiles(SelectDescriptors(descriptorActions, DescriptorAction.DeleteFile));
        
        _logger.LogTrace("Updating cache files in restore point");
        await _restorePoint.BulkUpdateCacheFileDescriptorsAsync(
            SelectDescriptors(descriptorActions, DescriptorAction.SetRestore),
            SelectDescriptors(descriptorActions, DescriptorAction.UnsetRestore));

        // Update the memory queue
        var enqueueDescriptors = SelectDescriptors(descriptorActions, DescriptorAction.Enqueue);
        if (enqueueDescriptors.Any(descriptor => !_descriptorQueue.TryAdd(descriptor.Id, descriptor)))
        {
            throw new InvalidOperationException("Failed to enqueue partial span cache file descriptor.");
        }

        var dequeueDescriptors = SelectDescriptors(descriptorActions, DescriptorAction.Dequeue);
        if (dequeueDescriptors.Any(descriptor => !_descriptorQueue.TryRemove(descriptor.Id, out _)))
        {
            throw new InvalidOperationException("Failed to remove merged file descriptor.");
        }
    }

    private void DeleteConsumedCacheFiles(IEnumerable<CacheFileDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            _fileSystem.DeleteFile(descriptor.FilePath);
        }
    }

    private async Task WriteOutputFileAsync(IReadOnlyCollection<CacheFileDescriptor> queuedDescriptors)
    {
        if (queuedDescriptors.Count == 0)
            throw new InvalidOperationException();
        
        var columnSchema = _memoryCache.Get<ColumnSchema>(nameof(ColumnSchema))
                           ?? throw new InvalidOperationException();
        var dataChannel = _dataChannelFactory.CreateChannel(_outputOptions.OutputFormat);
        var mergeContext = dataChannel.CreateMergeContext(columnSchema);
        var mergedCount = 0;
        var lowOffset = (int?)null;
        
        // Merge cache data
        foreach (var descriptor in queuedDescriptors)
        {
            lowOffset ??= descriptor.CommitOffset;
            
            _logger.LogDebug("Merging cached data (range {start}->{finish}) to bulk output file",
                descriptor.CommitOffset,
                descriptor.CommitOffset + descriptor.ActualCount-1);
            
            await _fileSystem.ReadFromStreamAsync(descriptor.FilePath, async stream =>
            {
                return await mergeContext.MergeFromStreamAsync(
                    stream,
                    descriptor.MergeOffset,
                    descriptor.ActualCount,
                    CancellationToken.None);
            });

            mergedCount += descriptor.ActualCount;
        }

        // Write the data file
        var committedOffset = lowOffset!.Value + mergedCount;
        var path = dataChannel.CreateFileName($"{_table}_{lowOffset:0000000000}");
        var outputFileInfo = await _fileSystem.WriteToStreamAsync(path, mergeContext, async (stream, context) =>
            await context.WriteToStreamAsync(stream, CancellationToken.None));
        
        _logger.LogInformation("Wrote output file {path} with records {start}->{finish}",
            path,
            lowOffset, 
            committedOffset-1);
        
        // Update snapshot
        await _restorePoint.UpdateCommittedOffsetAsync(_committedOffset = committedOffset);

        _telemetryListener.Increment("OutputAggregator.FilesWritten");
        _telemetryListener.Increment("OutputAggregator.BytesWritten", outputFileInfo.Length);
        _telemetryListener.TrackNumericValue("FileSystem.OutputFileSize (bytes)", outputFileInfo.Length);
        
        await Task.CompletedTask;
    }

    private IReadOnlyList<CacheFileDescriptor> BuildOrderedDescriptorCollection()
    {
        var queueSnapshot = _descriptorQueue.ToArray();
        var sortedDescriptors = queueSnapshot
            .Select(kv => kv.Value)
            .OrderBy(dsc => dsc.CommitOffset)
            .ToArray();
        var list = new List<CacheFileDescriptor>();
        var offset = _committedOffset;

        foreach (var descriptor in sortedDescriptors)
        {
            if (descriptor.CommitOffset != offset)
                break;
            
            list.Add(descriptor);
            offset += descriptor.ActualCount;
        }
        
        return list;
    }

    private void ThrowTimerExceptions()
    {
        if (_timerExceptions.Any())
        {
            throw new AggregateException(_timerExceptions);
        }
    }
}