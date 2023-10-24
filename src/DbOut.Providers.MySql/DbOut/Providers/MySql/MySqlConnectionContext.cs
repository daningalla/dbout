using System.Diagnostics;
using DbOut.Data;
using DbOut.Metadata;
using DbOut.Options;
using DbOut.Providers.MySql.Internal;
using DbOut.Reporting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DbOut.Providers.MySql;

public class MySqlConnectionContext : IConnectionContext
{
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ITelemetryListener _telemetryListener;
    private readonly ConnectionFactory _connectionFactory;
    private readonly DataSourceOptions _datasource;

    public MySqlConnectionContext(
        ILogger logger,
        RuntimeOptions options,
        IMemoryCache memoryCache,
        ITelemetryListener telemetryListener)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _telemetryListener = telemetryListener;
        _datasource = RuntimeOptions.ThrowIfNullReference("Options.Datasource", options.DataSource);
        _connectionFactory = new ConnectionFactory(logger, options, telemetryListener);
    }

    /// <inheritdoc />
    public async Task HealthCheckAsync(CancellationToken cancellationToken)
    {
        using var _ = BeginConnectionLoggingScope(_logger);
        await using var connection = await _connectionFactory.CreateAndOpenAsync(cancellationToken);

        var policy = PolicyFactory.CreateRetryPolicy<Exception>(
            nameof(MySqlDatabaseProvider),
            "QueryHealthCheckResult",
            _logger);

        AddServiceTelemetry();
        await connection.QueryAndLogAsync<int>(_logger, policy, "SELECT 1");
    }

    /// <inheritdoc />
    public async Task<ColumnSchema> GetColumnSchemaAsync(
        Predicate<string> columnNamePredicate,
        CancellationToken cancellationToken)
    {
        var result = await _memoryCache.GetOrCreateAsync($"{nameof(MySqlConnectionContext)}.ColumnSchema",
            async entry =>
            {
                await using var connection = await _connectionFactory.CreateAndOpenAsync(cancellationToken);

                var schemaBuilder = new ColumnSchemaBuilder(
                    _logger,
                    _datasource,
                    columnNamePredicate);

                entry.Value = schemaBuilder;
        
                return await schemaBuilder.BuildAsync(connection, cancellationToken);
            });

        AddServiceTelemetry();
        return result!;
    }

    /// <inheritdoc />
    public async Task<object?> GetWatermarkValueAsync(
        ColumnMetadata watermarkColumnMetadata,
        int offset,
        CancellationToken cancellationToken)
    {
        var query = new WatermarkColumnQuery(_logger, _connectionFactory, _datasource, watermarkColumnMetadata);
        AddServiceTelemetry();
        return await query.ExecuteAsync(offset, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Recordset> GetRecordsetAsync(
        RecordsetBufferPool recordsetBufferPool,
        ColumnSchema columnSchema,
        ColumnMetadata watermarkColumnMetadata,
        int offset,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var query = new OffsetDataQuery(
            _logger,
            _connectionFactory,
            _datasource,
            recordsetBufferPool,
            columnSchema,
            watermarkColumnMetadata,
            _telemetryListener);

        var results = await query.ExecuteAsync(offset, batchSize, cancellationToken);

        _telemetryListener.TrackNumericValue("Performance.MySqlOffsetQueryTime (ms)", stopwatch.ElapsedMilliseconds);
        _telemetryListener.Increment("MySql.RecordsRead", results.RowCount);
        AddServiceTelemetry();
        
        return results;
    }

    public static IDisposable? BeginConnectionLoggingScope(ILogger logger)
    {
        return logger.BeginScope("MySql {id}", Guid.NewGuid().ToString()[^4..]);
    }

    private void AddServiceTelemetry()
    {
        _telemetryListener.PushHashEntry("Services.DatabaseProvider", nameof(MySqlDatabaseProvider));
    }
}