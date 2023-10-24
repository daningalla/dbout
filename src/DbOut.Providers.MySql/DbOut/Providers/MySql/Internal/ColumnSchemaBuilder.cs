using DbOut.Exceptions;
using DbOut.Metadata;
using DbOut.Options;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace DbOut.Providers.MySql.Internal;

internal class ColumnSchemaBuilder
{
    private readonly ILogger _logger;
    private readonly DataSourceOptions _dataSource;
    private readonly Predicate<string> _columnNamePredicate;

    internal ColumnSchemaBuilder(ILogger logger, DataSourceOptions dataSource, Predicate<string> columnNamePredicate)
    {
        _logger = logger;
        _dataSource = dataSource;
        _columnNamePredicate = columnNamePredicate;
    }
    
    public async Task<ColumnSchema> BuildAsync(MySqlConnection connection, CancellationToken _)
    {
        var table = RuntimeOptions.ThrowIfNullOrEmpty("Options.DataSource.Table", _dataSource.TableName);
        var schema = _dataSource.Schema ?? connection.Database;
        IReadOnlyList<dynamic> results;
        
        using (MySqlConnectionContext.BeginConnectionLoggingScope(_logger))
        {
            var sql = MySqlResources.InformationQueryStatement;
            var policy = PolicyFactory.CreateRetryPolicy<Exception>(
                nameof(MySqlDatabaseProvider),
                "QueryInformationSchema",
                _logger,
                _dataSource.CommandRetryIntervals);
            results = await connection.QueryAndLogAsync<dynamic>(_logger, policy, sql, new
            {
                schema,
                table
            });
        }

        var metadataEntries = results
            .Where(result => _columnNamePredicate((string)result.COLUMN_NAME))
            .Select(result => BuildColumnMetadata(
                schema,
                (string)result.COLUMN_NAME,
                (uint)result.ORDINAL_POSITION,
                (string)result.DATA_TYPE,
                (ulong?)result.CHARACTER_MAXIMUM_LENGTH,
                (string)result.IS_NULLABLE,
                (string)result.COLUMN_KEY));

        return new ColumnSchema(schema, table, metadataEntries);
    }

    private ColumnMetadata BuildColumnMetadata(
        string schema,
        string columnName,
        uint ordinalPosition,
        string dataType,
        ulong? characterMaximumLength,
        string isNullable,
        string columnKey)
    {
        var isNullableBool = isNullable == "YES";
        var converter = TryCreateColumnValueConverter(dataType, characterMaximumLength, isNullableBool);

        if (converter != null)
        {
            return new ColumnMetadata(
                columnName,
                converter.ValueType,
                converter.AnnotatedValueType,
                isNullableBool,
                (int)ordinalPosition,
                GetColumnIndexType(columnKey),
                converter);
        }

        _logger.LogError(
            "Unsupported column type {type} (nullable={nullable}) encountered in column schema {schema}.{table}.{column}",
            dataType,
            isNullable,
            schema,
            _dataSource.TableName,
            columnName);
        
        throw new CoreStopException();
    }

    private static ColumnIndexType GetColumnIndexType(string columnKey)
    {
        return columnKey switch
        {
            "PRI" => ColumnIndexType.PrimaryKey,
            "UNI" => ColumnIndexType.Unique,
            "MUL" => ColumnIndexType.NonClustered,
            _ => ColumnIndexType.None
        };
    }

    private static IValueConverter? TryCreateColumnValueConverter(
        string dataType,
        ulong? characterMaximumLength,
        bool isNullable)
    {
        switch (dataType)
        {
            case "tinyint":
                return ValueConverter.Create(isNullable, src => src != 0, src => Convert.ToSByte(Convert.ToBoolean(src)));
            case "smallint":
                return ValueConverter.Create(isNullable, short.Parse);
            case "int":
            case "mediumint":
            case "integer":
                return ValueConverter.Create(isNullable, int.Parse);
            case "bigint":
                return ValueConverter.Create(isNullable, long.Parse);
            case "decimal":
            case "numeric":
                return ValueConverter.Create(isNullable, decimal.Parse);
            case "float":
                return ValueConverter.Create(isNullable, float.Parse);
            case "double":
                return ValueConverter.Create(isNullable, double.Parse);
            case "date":
            case "datetime":
            case "timestamp":
            case "time":
            case "year":
                return ValueConverter.Create(isNullable, DateTime.Parse);
            case "char" when characterMaximumLength == 36:
                return ValueConverter.Create(isNullable, Guid.Parse);
            case "char":
            case "varchar":
            case "enum":
            case "json":
            case "text":
            case "tinytext":                
            case "mediumtext":                
            case "longtext":
                return ValueConverter.Create<string>(isNullable, str => str);
            
            default:
                return null;
        }
    }
}