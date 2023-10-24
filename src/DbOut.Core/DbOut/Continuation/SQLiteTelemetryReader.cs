using System.Data.SQLite;
using Dapper;
using DbOut.Reporting;

namespace DbOut.Continuation;

internal static class SQLiteTelemetryReader
{
    public static async Task ReadAsync(SQLiteConnection connection, SummaryTelemetryData telemetryData)
    {
        var gridReader = await connection.QueryMultipleAsync(Resources.SelectTelemetryStatement);
        var counters = await gridReader.ReadAsync();
        foreach (var counter in counters)
        {
            telemetryData.Counters.Add((string)counter.Key, (long)counter.Value);
        }

        var numerics = await gridReader.ReadAsync();
        foreach (var numeric in numerics)
        {
            telemetryData.Numerics.Add((string)numeric.Key, new TrackedNumeric
            {
                Min = (double)numeric.Min,
                Max = (double)numeric.Max,
                MeanCount = (int)numeric.MeanCount,
                MeanTotal = (double)numeric.MeanTotal,
                Value = (double)numeric.Value
            });
        }

        var keyValues = await gridReader.ReadAsync();
        foreach (var keyValue in keyValues)
        {
            var entry = new KeyValuePair<string, string>((string)keyValue.Key, (string)keyValue.Value);
            if ((string)keyValue.Type == "list")
                telemetryData.Lists.Add(entry);
            else
                telemetryData.HashSets.Add(entry);
        }
    }
}