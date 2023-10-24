using DbOut.Data;
using DbOut.Metadata;

namespace DbOut.Providers;

public interface IConnectionContext
{
    Task HealthCheckAsync(CancellationToken cancellationToken);

    Task<ColumnSchema> GetColumnSchemaAsync(Predicate<string> columnNamePredicate, CancellationToken cancellationToken);

    Task<object?> GetWatermarkValueAsync(
        ColumnMetadata watermarkColumnMetadata,
        int offset,
        CancellationToken cancellationToken);

    Task<Recordset> GetRecordsetAsync(
        RecordsetBufferPool recordsetBufferPool,
        ColumnSchema columnSchema,
        ColumnMetadata watermarkColumnMetadata,
        int offset,
        int batchSize,
        CancellationToken cancellationToken);
}