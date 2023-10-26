using System.Data.Common;
using DbOut.Data;
using DbOut.Metadata;
using DbOut.Options;
using DbOut.Reporting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace DbOut.Providers.MySql.Internal;

internal sealed class OffsetDataQuery
{
    private static int LogFirstRow;
    private readonly ILogger _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly DataSourceOptions _datasource;
    private readonly RecordsetBufferPool _recordsetBufferPool;
    private readonly ColumnSchema _columnSchema;
    private readonly ColumnMetadata _watermarkColumnMetadata;
    private readonly ITelemetryListener _telemetryListener;

    public OffsetDataQuery(
        ILogger logger,
        ConnectionFactory connectionFactory,
        DataSourceOptions datasource,
        RecordsetBufferPool recordsetBufferPool,
        ColumnSchema columnSchema,
        ColumnMetadata watermarkColumnMetadata,
        ITelemetryListener telemetryListener)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _datasource = datasource;
        _recordsetBufferPool = recordsetBufferPool;
        _columnSchema = columnSchema;
        _watermarkColumnMetadata = watermarkColumnMetadata;
        _telemetryListener = telemetryListener;
    }

    public async Task<Recordset> ExecuteAsync(int offset, int batchSize, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateAndOpenAsync(cancellationToken);
        var schema = _datasource.Schema ?? connection.Database;
        var table = RuntimeOptions.ThrowIfNullOrEmpty("Options.Datasource.TableName", _datasource.TableName);
        var columnList = string.Join(',', _columnSchema.Select(column => column.ColumnName));
        
        using var _ = MySqlConnectionContext.BeginConnectionLoggingScope(_logger);

        await using var command = connection.CreateCommand();
        
        command.CommandText = MySqlResources.OffsetQueryStatement
            .Replace("$(column-list)", columnList)
            .Replace("$(schema)", schema)
            .Replace("$(table)", table)
            .Replace("$(watermark-column)", _watermarkColumnMetadata.ColumnName);
        
        command.Parameters.AddRange(new[]
        {
            new MySqlParameter("limit", batchSize),
            new MySqlParameter("offset", offset)
        });

        var policy = PolicyFactory.CreateRetryPolicy<Exception>(
            nameof(MySqlDatabaseProvider),
            "execute offset query",
            _logger,
            _datasource.CommandRetryIntervals,
            (exception, attempt) =>
            {
                _telemetryListener.Increment("MySql.QueryRetries");
                _telemetryListener.TrackNumericValue("Resilience.MySqlQueryRetryAttempt", attempt);
                _telemetryListener.Increment($"MySql.Exceptions.{exception.GetType().Name}");
            });

        await using var reader = await policy.ExecuteAsync(() => command.ExecuteReaderAndLogAsync(
            _logger, cancellationToken));

        var result = await RecordsetAdapter.FillRecordsetAsync(
            reader,
            _columnSchema,
            _recordsetBufferPool.GetInstance(),
            LoadSampleReaderRow,
            cancellationToken);

        _logger.LogDebug("Query command read {count} rows.", result.RowCount);
        
        return result;
    }

    private void LoadSampleReaderRow(DbDataReader reader)
    {
        if (Interlocked.Increment(ref LogFirstRow) != 1)
            return;

        using var _ = _logger.BeginScope("Source metadata");
        
        for (var c = 0; c < reader.FieldCount; c++)
        {
            var fieldType = reader.GetFieldType(c);
            _logger.LogTrace("{ordinal} {name} = {type}",
                c+1,
                reader.GetName(c),
                fieldType.Name);
        }
    }
}