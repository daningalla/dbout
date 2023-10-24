using System.Collections.Concurrent;
using System.Diagnostics;
using DbOut.Data;
using DbOut.Metadata;
using DbOut.Reporting;
using Microsoft.Extensions.Logging;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace DbOut.DataChannels;

public class ParquetDataChannel : IDataChannel
{
    private static readonly ConcurrentDictionary<Guid, ParquetSchema> CachedSchemas = new(); 
    private readonly ILogger<ParquetDataChannel> _logger;
    private readonly ITelemetryListener _telemetryListener;

    public ParquetDataChannel(ILogger<ParquetDataChannel> logger, ITelemetryListener telemetryListener)
    {
        _logger = logger;
        _telemetryListener = telemetryListener;
    }

    /// <inheritdoc />
    public string FormatType => "parquet";

    /// <inheritdoc />
    public string CreateFileName(string file)
    {
        return $"{file}.parquet";
    }

    /// <inheritdoc />
    public async Task WriteToStreamAsync(
        Stream stream,
        Recordset recordset,
        int rowIndex,
        int count,
        CancellationToken cancellationToken)
    {
        AddServiceTelemetry();
        
        var parquetSchema = BuildParquetSchema(recordset.ColumnSchema);
        var stopwatch = Stopwatch.StartNew();
        
        using var parquetWriter = await ParquetWriter.CreateAsync(
            parquetSchema,
            stream,
            cancellationToken: cancellationToken);

        using var groupWriter = parquetWriter.CreateRowGroup();

        for (var c = 0; c < recordset.ColumnSchema.Count; c++)
        {
            var dataField = parquetSchema.DataFields[c];
            var array = recordset.GetColumnData(c, rowIndex, count);
            var dataColumn = new DataColumn(dataField, array);
            await groupWriter.WriteColumnAsync(dataColumn, cancellationToken);
        }
        
        _logger.LogDebug("Wrote {columns} columns in {rows} rows to parquet stream.",
            recordset.ColumnSchema.Count,
            recordset.RowCount);
        
        _telemetryListener.TrackNumericValue("Performance.ParquetStreamSerializationTime (ms)", 
            stopwatch.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public IDataMergeContext CreateMergeContext(ColumnSchema columnSchema)
    { 
        AddServiceTelemetry();
        var parquetSchema = BuildParquetSchema(columnSchema);
        return new ParquetDataMergeContext(parquetSchema);
    }

    private ParquetSchema BuildParquetSchema(ColumnSchema columnSchema)
    {
        return CachedSchemas.GetOrAdd(columnSchema.SchemaId, id =>
        {
            _logger.LogDebug("Created parquet schema for column schema {id}", id);
            return new ParquetSchema(columnSchema.Select(CreateDataField));
        });
    }

    private void AddServiceTelemetry()
    {
        _telemetryListener.PushHashEntry("Services.DataChannel", nameof(ParquetDataChannel));
    }

    private static DataField CreateDataField(ColumnMetadata columnMetadata)
    {
        return new DataField(
            columnMetadata.ColumnName,
            columnMetadata.DataType,
            columnMetadata.IsNullable);
    }
}