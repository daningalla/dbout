using DbOut.Continuation;
using DbOut.IO;
using DbOut.Reporting;
using DbOut.Services;
using DbOut.Utilities;
using Microsoft.Extensions.Logging;

namespace DbOut.Data;

[Service]
public class PartitionedQueryHandler : IPartitionedQueryHandler
{
    private readonly ILogger<PartitionedQueryHandler> _logger;
    private readonly IRuntimeServices _runtimeServices;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryListener _telemetryListener;
    private readonly IRestorePoint _restorePoint;
    private readonly IOutputAggregator _outputAggregator;

    public PartitionedQueryHandler(
        ILogger<PartitionedQueryHandler> logger, 
        IRuntimeServices runtimeServices,
        IFileSystem fileSystem,
        ITelemetryListener telemetryListener,
        IRestorePoint restorePoint,
        IOutputAggregator outputAggregator)
    {
        _logger = logger;
        _runtimeServices = runtimeServices;
        _fileSystem = fileSystem;
        _telemetryListener = telemetryListener;
        _restorePoint = restorePoint;
        _outputAggregator = outputAggregator;
    }
    
    /// <inheritdoc />
    public async Task<int> QueryAsync(PartitionedQuery query, CancellationToken cancellationToken)
    {
        var sha = query.Sha();

        var cachedFiles = await _restorePoint.GetCacheFileDescriptorsAsync(sha);
        if (cachedFiles.Count > 0)
        {
            foreach (var cacheFile in cachedFiles)
            {
                var cacheFilePath = Path.Combine(_fileSystem.BasePath, cacheFile.FilePath);
                var cacheFileHash = await HashUtilities.HashFileAsync(new FileInfo(cacheFilePath));

                if (cacheFileHash != cacheFile.ContentHash) 
                    continue;
                
                await _outputAggregator.EnqueueCachedFileAsync(cacheFile, isFromRestorePoint: true);
                _telemetryListener.Increment("FileSystem.CacheFilesUsed", cachedFiles.Count);
            }
            
            return cachedFiles.Sum(file => file.ActualCount);
        }
        
        var recordset = await QueryRecordsetAsync(query, cancellationToken);

        if (recordset.RowCount == 0)
        {
            _logger.LogDebug("Partitioned query return no records (limit {limit} offset {offset})", 
                query.BatchSize, query.Offset);
            
            return 0;
        }

        await WriteCacheFileAsync(query, recordset, sha, cancellationToken);

        return recordset.RowCount;
    }

    private async Task<Recordset> QueryRecordsetAsync(
        PartitionedQuery query,
        CancellationToken cancellationToken)
    {
        var recordset = await _runtimeServices.ConnectionContext.GetRecordsetAsync(
            query.RecordsetBufferPool,
            query.ColumnSchema,
            query.WatermarkColumnMetadata,
            query.Offset,
            query.BatchSize,
            cancellationToken);

        return recordset;
    }

    private async Task WriteCacheFileAsync(
        PartitionedQuery query,
        Recordset recordset,
        string sha,
        CancellationToken cancellationToken)
    {
        var channel = _runtimeServices.DataChannel;
        var fileName = channel.CreateFileName(sha);
        var cacheFileSystem = FileSystemConventions.CreateCacheFileSystem(_fileSystem);
        var fileInfo = await cacheFileSystem.WriteToStreamAsync(
            fileName,
            recordset,
            async (stream, data) => await channel.WriteToStreamAsync(
                stream,
                data,
                0,
                recordset.RowCount,
                cancellationToken));

        var basePath = cacheFileSystem.Root.BasePath;
        var relativePath = Path.GetRelativePath(basePath, fileInfo.FullName);
        var fileDescriptor = new CacheFileDescriptor
        {
            Id = Guid.NewGuid().ToString(),
            QueryHash = sha,
            SubsetId = 0,
            FilePath = relativePath,
            FileType = channel.FormatType,
            CompressionType = cacheFileSystem.Compression,
            CommitOffset = query.Offset,
            MergeOffset = 0,
            QueryCount = query.BatchSize,
            ActualCount = recordset.RowCount,
            ContentHash = await HashUtilities.HashFileAsync(fileInfo),
            SizeInBytes = fileInfo.Length
        };

        await _outputAggregator.EnqueueCachedFileAsync(fileDescriptor, isFromRestorePoint: false);
        
        _telemetryListener.Increment("FileSystem.CacheFilesWritten");
        _telemetryListener.Increment("FileSystem.CacheFileWrittenBytes", fileInfo.Length);
        _telemetryListener.TrackNumericValue("FileSystem.CacheFileSize (bytes)", fileInfo.Length);
    }
}