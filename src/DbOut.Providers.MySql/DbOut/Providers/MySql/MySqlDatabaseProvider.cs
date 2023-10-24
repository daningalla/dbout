using DbOut.Options;
using DbOut.Reporting;
using DbOut.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DbOut.Providers.MySql;

[Service]
public class MySqlDatabaseProvider : IDatabaseProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ITelemetryListener _telemetryListener;
    private readonly ILogger<MySqlDatabaseProvider> _logger;

    public MySqlDatabaseProvider(
        ILoggerFactory loggerFactory,
        IMemoryCache memoryCache,
        ITelemetryListener telemetryListener)
    {
        _loggerFactory = loggerFactory;
        _memoryCache = memoryCache;
        _telemetryListener = telemetryListener;
        _logger = loggerFactory.CreateLogger<MySqlDatabaseProvider>();
    }

    /// <inheritdoc />
    public string ProviderId => nameof(MySqlDatabaseProvider);

    /// <inheritdoc />
    public IConnectionContext CreateConnectionContext(RuntimeOptions options)
    {
        _logger.LogTrace("Creating MySql connection context");
        
        return new MySqlConnectionContext(
            _loggerFactory.CreateLogger<MySqlConnectionContext>(),
            options,
            _memoryCache,
            _telemetryListener);
    }
}