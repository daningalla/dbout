using DbOut.Metadata;
using DbOut.Options;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace DbOut.Providers.MySql.Internal;

internal class WatermarkColumnQuery
{
    private readonly ILogger _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly DataSourceOptions _dataSource;
    private readonly ColumnMetadata _watermarkColumnSchema;

    public WatermarkColumnQuery(
        ILogger logger,
        ConnectionFactory connectionFactory,
        DataSourceOptions dataSource,
        ColumnMetadata watermarkColumnSchema)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _dataSource = dataSource;
        _watermarkColumnSchema = watermarkColumnSchema;
    }

    public async Task<object?> ExecuteAsync(int offset, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateAndOpenAsync(cancellationToken);
        var schema = _dataSource.Schema ?? connection.Database;
        var table = RuntimeOptions.ThrowIfNullOrEmpty("Options.DataSource.Table", _dataSource.TableName);

        var sql = MySqlResources.WatermarkValueQueryStatement
            .Replace("$(column)", _watermarkColumnSchema.ColumnName)
            .Replace("$(schema)", schema)
            .Replace("$(table)", table);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new MySqlParameter("offset", offset));
        
        using var _ = MySqlConnectionContext.BeginConnectionLoggingScope(_logger);
        await using var reader = await command.ExecuteReaderAndLogAsync(_logger, cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return reader.GetValue(0);
        }

        return null;
    }
}