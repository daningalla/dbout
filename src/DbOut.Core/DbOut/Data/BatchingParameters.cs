using DbOut.Metadata;
using DbOut.Options;

namespace DbOut.Data;

public record BatchingParameters(
    ColumnSchema ColumnSchema,
    ColumnMetadata WatermarkColumnMetadata,
    int RecordOffset,
    int MaxRowCount,
    ParallelizationOptions Parallelization);