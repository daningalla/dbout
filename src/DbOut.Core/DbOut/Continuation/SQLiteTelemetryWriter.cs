using System.Data.SQLite;
using Dapper;
using DbOut.Reporting;

namespace DbOut.Continuation;

internal static class SQLiteTelemetryWriter
{
    internal static async Task WriteAsync(SQLiteConnection connection, SummaryTelemetryData telemetryData)
    {
        await using var transaction = await connection.BeginTransactionAsync();
                
                    await connection.ExecuteAsync(Resources.DeleteTelemetryStatement, transaction: transaction);
                
                    var sql = Resources.InsertTelemetryCounterStatement;
                    foreach (var counter in telemetryData.Counters)
                    {
                        await connection.ExecuteAsync(sql, new
                        {
                            counter.Key,
                            counter.Value
                        }, transaction);
                    }
        
                    sql = Resources.InsertTelemetryNumericStatement;
                    foreach (var numeric in telemetryData.Numerics)
                    {
                        await connection.ExecuteAsync(sql, new
                        {
                            numeric.Key,
                            numeric.Value.Min,
                            numeric.Value.Max,
                            numeric.Value.MeanCount,
                            numeric.Value.MeanTotal,
                            numeric.Value.Value
                        }, transaction);
                    }
        
                    sql = Resources.InsertTelemetryKeyValueStatement;
                    foreach (var item in telemetryData.Lists)
                    {
                        await connection.ExecuteAsync(sql, new
                        {
                            item.Key,
                            Type = "list",
                            item.Value
                        }, transaction);
                    }
        
                    foreach (var item in telemetryData.HashSets)
                    {
                        await connection.ExecuteAsync(sql, new
                        {
                            item.Key,
                            Type = "set-item",
                            item.Value
                        }, transaction);
                    }
        
                    await transaction.CommitAsync();
    }
}