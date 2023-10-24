using System.Data.SQLite;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using DbOut.IO;
using DbOut.Options;
using DbOut.Reporting;
using DbOut.Services;
using DbOut.Utilities;
using Microsoft.Extensions.Logging;

namespace DbOut.Continuation;

[Service]
public sealed class SQLiteRestorePoint : IRestorePoint, IAsyncDisposable
{
    private static readonly JsonSerializerOptions ParameterSerializerOptions = new()
    {
        Converters =
        {
            FileSize.JsonConverter,
            new JsonStringEnumConverter()
        }
    };
    private readonly ILogger<SQLiteRestorePoint> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly SemaphoreSlim _lock = new(1);

    public SQLiteRestorePoint(ILogger<SQLiteRestorePoint> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CacheFileDescriptor>> GetCacheFileDescriptorsAsync(string queryHash)
    {
        return await ExecuteWhileLockedAsync(async connection =>
        {
            var result = await connection.QueryAsync<CacheFileDescriptor>(Resources.SelectCacheFileDescriptorStatement,
                new { queryHash });

            return result.ToArray();
        });
    }

    public async Task SaveRuntimeParametersAsync(RuntimeOptions options)
    {
        await ExecuteWhileLockedAsync(async connection =>
        {
            var sha = options.Sha();
            var json = JsonSerializer.Serialize(options, ParameterSerializerOptions);
            var sql = Resources.InsertJobParametersStatement;

            await connection.ExecuteAsync(sql, new { Hash = sha, Parameters = json });
            return 0;
        });
    }

    /// <inheritdoc />
    public async Task<RestorePointParameters?> GetRuntimeParametersAsync()
    {
        return await ExecuteWhileLockedAsync(async connection =>
        {
            var result = await connection.QueryAsync(Resources.SelectJobParametersStatement);
            var first = result.SingleOrDefault();

            return first != null
                ? new RestorePointParameters(
                    first.Hash,
                    JsonSerializer.Deserialize<RuntimeOptions>(first.Parameters, ParameterSerializerOptions))
                : null;
        });
    }

    public async Task UpdateCommittedOffsetAsync(int value)
    {
        await ExecuteWhileLockedAsync(async connection =>
        {
            await connection.ExecuteAsync(Resources.UpsertJobSnapshotStatement, new { CommittedOffset = value });
            return 0;
        });
    }

    public async Task<int?> GetCommittedOffsetAsync()
    {
        return await ExecuteWhileLockedAsync(async connection =>
        {
            var result = await connection.QueryAsync(Resources.SelectJobSnapshotStatement);
            return (int?)result.FirstOrDefault()?.CommittedOffset;
        });
    }

    /// <inheritdoc />
    public RuntimeOptions CreateCriticalOptions(RuntimeOptions options)
    {
        return new RuntimeOptions
        {
            Output = options.Output,
            ConnectionOptions = new ConnectionOptions
            {
                Provider = options.ConnectionOptions?.Provider,
                Server = options.ConnectionOptions?.Server,
                Database = options.ConnectionOptions?.Database,
                ConnectionString = options.ConnectionOptions?.ConnectionString,
            },
            DataSource =  new DataSourceOptions
            {
                Schema  = options.DataSource?.Schema,
                TableName = options.DataSource?.TableName,
                WatermarkColumnName = options.DataSource?.WatermarkColumnName,
                ExcludedColumns = options.DataSource?.ExcludedColumns,
                SelectColumns = options.DataSource?.SelectColumns
            },
            CommandMode = CommandMode.Execute
        };
    }

    public async Task AddCacheFileDescriptorAsync(CacheFileDescriptor fileDescriptor)
    {
        await ExecuteWhileLockedAsync(async connection =>
        {
            var sql = Resources.InsertCacheFileDescriptorStatement;

            await connection.ExecuteAsync(sql, param: fileDescriptor);
            _logger.LogTrace("Inserted file descriptor {path} into restore point", fileDescriptor);
            return 0;
        });
    }

    public async Task<SummaryTelemetryData> LoadTelemetryAsync()
    {
        var data = new SummaryTelemetryData();
        await ExecuteWhileLockedAsync(async connection =>
        {
            await SQLiteTelemetryReader.ReadAsync(connection, data);
            return 0;
        });
        return data;
    }
    
    public async Task SaveTelemetryAsync(SummaryTelemetryData telemetryData)
    {
        await ExecuteWhileLockedAsync(async connection =>
        {
            await SQLiteTelemetryWriter.WriteAsync(connection, telemetryData);
            return 0;
        });
    }

    /// <inheritdoc />
    public async Task BulkUpdateCacheFileDescriptorsAsync(
        IEnumerable<CacheFileDescriptor> insertDescriptors,
        IEnumerable<CacheFileDescriptor> deleteDescriptors)
    {
        await ExecuteWhileLockedAsync(async connection =>
        {
            await using var transaction = await connection.BeginTransactionAsync();
            
            var insertSql = Resources.InsertCacheFileDescriptorStatement;
            var deleteSql = Resources.DeleteCacheFileDescriptorStatement;
            
            foreach (var descriptor in insertDescriptors)
            {
                await connection.ExecuteAsync(insertSql, descriptor, transaction);
            }

            foreach (var descriptor in deleteDescriptors)
            {
                await connection.ExecuteAsync(deleteSql, new { descriptor.Id }, transaction);
            }

            await transaction.CommitAsync();
            return 0;
        });
    }

    private async Task InitializeDatabaseAsync()
    {
        var myFileSystem = FileSystemConventions.CreateRestorePointFileSystem(_fileSystem);
        var dbFilePath = Path.Combine(myFileSystem.BasePath, "db.sqlite");
        await using var connection = new SQLiteConnection($"Data Source={dbFilePath}");

        _logger.LogDebug("Opening SQLite connection {path}", dbFilePath);
        myFileSystem.EnsurePathCreated();
        await connection.OpenAsync();
        
        _logger.LogDebug("Creating restore point schema");
        await connection.ExecuteAsync(Resources.CreateRestorePointSchemaStatement);
    }

    private async Task<T> ExecuteWhileLockedAsync<T>(Func<SQLiteConnection, Task<T>> command)
    {
        await _lock.WaitAsync();

        try
        {
            return await command(GetConnection());
        }
        finally
        {
            _lock.Release();
        }
    }

    private SQLiteConnection GetConnection()
    {
        var myFileSystem = FileSystemConventions.CreateRestorePointFileSystem(_fileSystem);
        var dbFilePath = Path.Combine(myFileSystem.BasePath, "db.sqlite");
        var connection = new SQLiteConnection($"Data Source={dbFilePath}");
        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await ValueTask.CompletedTask;
}