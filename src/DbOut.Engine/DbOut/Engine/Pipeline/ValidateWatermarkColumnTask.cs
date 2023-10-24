using DbOut.Exceptions;
using DbOut.Metadata;
using DbOut.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class ValidateWatermarkColumnTask : CommandTask
{
    private readonly IMemoryCache _memoryCache;

    /// <inheritdoc />
    public ValidateWatermarkColumnTask(
        ILogger<ValidateDatabaseProviderTask> logger,
        IMemoryCache memoryCache) 
        : base(logger, new[] { CommandMode.Execute })
    {
        _memoryCache = memoryCache;
    }

    /// <inheritdoc />
    protected override Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        var schema = _memoryCache.Get<ColumnSchema>(nameof(ColumnSchema))
                     ?? throw new InvalidOperationException();
        
        var waterMarkColumn = RuntimeOptions.ThrowIfNullOrEmpty(
            "Options.DataSource.WatermarkColumnName",
            context.Options.DataSource?.WatermarkColumnName);

        if (!schema.TryGetColumn(waterMarkColumn, out var metadata))
        {
            Logger.LogError(
                "Could not resolve watermark column {name} from metadata. It may have been filtered by runtime options.",
                waterMarkColumn);
            throw new CoreStopException();
        }

        if (metadata.IsNullable)
        {
            Logger.LogError(
                "Can not use nullable column {name} as watermark (non-null column required).",
                waterMarkColumn);
            throw new CoreStopException();
        }

        if (metadata.IndexType == ColumnIndexType.None)
        {
            Logger.LogWarning(
                "Watermark column {name} is not indexed, query performance may be affected.", waterMarkColumn);
        }
        
        Logger.LogInformation("Validated watermark column {name}, index={index}",
            waterMarkColumn,
            metadata.IndexType);

        return Task.CompletedTask;
    }
}