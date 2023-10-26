using DbOut.IO;
using DbOut.Options;
using Microsoft.Extensions.Logging;
using Vertical.CommandLine;

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
            CleanMode = CleanConfirmMode.Accept
        };
    }

    public static ProgramArguments Debug()
    {
        var cli = """
                  
                     --provider MySqlDatabaseProvider `
                     --server onerail-core-db-prod-rr-east-2.mysql.database.azure.com `
                     --database onerail_core `
                     --user prodadmin@onerail-core-db-prod-rr-east-2 `
                     --password 84BTudvsXt4kWhkZYAYq2o6PYFvQNjod `
                     --provider-prop DefaultCommandTimeout=120 `
                     --log-level Trace `
                     --out $env:USERPROFILE\CoreDbExport\jobs\deliveries `
                     --max-file-size 10kb `
                     --compression None `
                     --schema onerail_core `
                     --table deliveries `
                     --watermark createdAt `
                     --batch-size 10 `
                     --threads 1 `
                     --exclude-columns "notes,shipperExtraData" `
                     --command-retry-intervals "1,1,1,1,1" `
                     --max-rows 100 `
                     --out-format Parquet `
                     --out-flush-interval 10
                  """;

        var args = cli.Split(Environment.NewLine)
            .Select(line => line.Replace(" `", "").Trim())
            .ToArray();

        return CommandLineApplication.ParseArguments<ProgramArguments>(new ProgramArgumentsConfiguration(), args);
    }
}