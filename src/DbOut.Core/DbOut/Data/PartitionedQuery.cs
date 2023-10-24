using DbOut.Metadata;
using DbOut.Utilities;

namespace DbOut.Data;

public class PartitionedQuery
{
    public Guid QueryId { get; } = Guid.NewGuid();
    public required ColumnSchema ColumnSchema { get; init; }
    public required ColumnMetadata WatermarkColumnMetadata { get; init; }
    public required RecordsetBufferPool RecordsetBufferPool { get; init; }
    public required int Offset { get; init; }
    public required int BatchSize { get; init; }

    public string Sha()
    {
        return new
        {
            Schema = ColumnSchema.Select(metadata => metadata.ColumnName),
            Watermark = WatermarkColumnMetadata.ColumnName,
            Offset,
            BatchSize
        }.Sha();
    }
}
    