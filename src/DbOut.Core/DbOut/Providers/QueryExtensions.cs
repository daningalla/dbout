using System.Data.Common;
using Dapper;
using DbOut.Utilities;
using Microsoft.Extensions.Logging;
using Polly.Retry;

namespace DbOut.Providers;

public static class QueryExtensions
{
    public static async Task<IReadOnlyList<T>> QueryAndLogAsync<T>(
        this DbConnection connection,
        ILogger logger,
        AsyncRetryPolicy retryPolicy,
        string sql,
        object? param = null)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Executing SQL query (driver={provider})\nStatement:\n{sql}\nParameters:\n\t{@parameters}",
                connection.GetType(),
                sql.Indent(1),
                param);
        }

        var results = await retryPolicy.ExecuteAsync(async () => 
            (await connection.QueryAsync<T>(sql, param)).ToList());
        
        logger.LogTrace("Result count = {count}", results.Count);

        return results;
    }

    public static async Task<DbDataReader> ExecuteReaderAndLogAsync(
        this DbCommand command,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            var paramString = string.Join(',', command.Parameters.Cast<DbParameter>().Select(parameter =>
                $"{parameter.ParameterName}={parameter.Value}"));
            logger.LogTrace("Execute SQL reader (driver={provider})\nStatement:\n{sql}\nParameters:\n\t{{ {parameters} }}",
                command.Connection?.GetType(),
                command.CommandText.Indent(1),
                paramString);
        }
        
        return await command.ExecuteReaderAsync(cancellationToken);
    }
}