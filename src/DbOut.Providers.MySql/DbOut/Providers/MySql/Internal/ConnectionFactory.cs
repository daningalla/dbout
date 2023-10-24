using System.Collections.Immutable;
using System.Diagnostics;
using DbOut.Exceptions;
using DbOut.Options;
using DbOut.Reporting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace DbOut.Providers.MySql.Internal;

internal class ConnectionFactory
{
    private readonly ILogger _logger;
    private readonly ITelemetryListener _telemetryListener;
    private readonly Lazy<CachedConnectionInfo> _lazyConnectionInfo;

    internal ConnectionFactory(ILogger logger, RuntimeOptions options, ITelemetryListener telemetryListener)
    {
        _logger = logger;
        _telemetryListener = telemetryListener;
        _lazyConnectionInfo = new Lazy<CachedConnectionInfo>(() => BuildConnectionString(options));
    }

    internal string Server => _lazyConnectionInfo.Value.Builder.Server;

    internal string Database => _lazyConnectionInfo.Value.Builder.Database;

    internal CachedConnectionInfo ConnectionInfo => _lazyConnectionInfo.Value;

    internal async Task<MySqlConnection> CreateAndOpenAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var connectionInfo = ConnectionInfo;
        var connection = new MySqlConnection(connectionInfo.ConnectionString);
        
        _logger.LogTrace("Connecting to {provider}:{server}/{database}",
            nameof(MySqlDatabaseProvider),
            connectionInfo.Builder.Server,
            connectionInfo.Builder.Database);
        
        await connectionInfo.ConnectRetryPolicy.ExecuteAsync(async () =>
            await connection.OpenAsync(cancellationToken));
        
        _logger.LogTrace("Connection opened");
        _telemetryListener.TrackNumericValue("Performance.MySqlConnectionOpenTime (ms)", stopwatch.ElapsedMilliseconds);

        return connection;
    }

    private CachedConnectionInfo BuildConnectionString(RuntimeOptions options)
    {
        var connectionValues = options.ConnectionOptions!;
        var builder = new MySqlConnectionStringBuilder();
        
        if (!string.IsNullOrWhiteSpace(connectionValues.ConnectionString))
            builder.ConnectionString = connectionValues.ConnectionString;
        else
        {
            builder.Server = RuntimeOptions.ThrowIfNullOrEmpty("Connection.Server", connectionValues.Server);
            builder.Database = RuntimeOptions.ThrowIfNullOrEmpty("Connection.Database", connectionValues.Database);
            builder.UserID = RuntimeOptions.ThrowIfNullOrEmpty("Connection.UserId", connectionValues.UserId);
            builder.Password = RuntimeOptions.ThrowIfNullOrEmpty("Connection.Password", connectionValues.Password);
        }

        var builderProperties = typeof(MySqlConnectionStringBuilder)
            .GetProperties()
            .ToDictionary(property => property.Name);

        foreach (var (key, value) in connectionValues.Properties ?? ImmutableDictionary<string, string>.Empty)
        {
            if (!builderProperties.TryGetValue(key, out var property))
            {
                _logger.LogError("Unsupported MySql connection property {key} (value='{value}')",
                    key,
                    value);
                throw new CoreStopException();
            }

            try
            {
                var convertedValue = Convert.ChangeType(value, property.PropertyType);
                
                property.SetValue(builder, convertedValue);
                _logger.LogTrace("Mapped value '{value}' to {propertyName} with conversion to {propertyType}.",
                    value,
                    $"{nameof(MySqlConnectionStringBuilder)}.{property.Name}",
                    property.GetType().Name);
            }
            catch
            {
                _logger.LogError("Could not map value '{value}' to {propertyName}, expected conversion to {propertyType}.",
                    value,
                    $"{nameof(MySqlConnectionStringBuilder)}.{property.Name}",
                    property.PropertyType.Name);
                throw new CoreStopException();
            }
        }

        return new CachedConnectionInfo
        {
            Builder = builder,
            ConnectionString = builder.ToString(),
            ConnectRetryPolicy = PolicyFactory.CreateRetryPolicy<Exception>(
                nameof(MySqlDatabaseProvider),
                "CreateAndOpenConnection",
                _logger,
                connectionValues.ConnectRetryIntervals,
                (exception, attempt) =>
                {
                    _telemetryListener.Increment("MySql.ConnectionOpenRetries");
                    _telemetryListener.TrackNumericValue("Resilience.MySqlConnectionRetryAttempt", attempt);
                    _telemetryListener.Increment($"MySql.Exceptions.{exception.GetType().Name}");
                })
        };
    }
}