using DbOut.IO;
using DbOut.Options;
using Microsoft.Extensions.Logging;

namespace DbOut.Console;

public static class DebugArguments
{
    public static ProgramArguments Create()
    {
        return new ProgramArguments
        {
            DatabaseProvider = "MySqlDatabaseProvider",
            ConnectionString = "Server=localhost;Database=db_export;User ID=root;Password=P@ssw0rd!",
            LogLevel = LogLevel.Trace,
            ProviderProperties =
            {
                ["DefaultCommandTimeout"] = "60"
            },
            SourceSchema = "db_export",
            SourceTable = "profile",
            WatermarkColumnName = "id",
            BatchSize = 1000,
            MaxThreads = 4,
            //MaxRows = 100,
            ExcludedColumns = { "thumbnail" },
            OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MySqlExport2"),
            OutputFormat = OutputFormat.Parquet,
            MaxFileSize = FileSize.Parse("1mb"),
            Compression = FileCompression.GZip,
            OutputFlushIntervalSeconds = 1,
            Clean = true
        };
    }
}