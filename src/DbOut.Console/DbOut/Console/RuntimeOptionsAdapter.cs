using DbOut.Options;
using Microsoft.Extensions.Options;

namespace DbOut.Console;

/// <summary>
/// Provides options.
/// </summary>
public class RuntimeOptionsAdapter : IOptions<RuntimeOptions>
{
    private readonly Lazy<RuntimeOptions> _lazyOptions;

    /// <summary>
    /// Creates a new instance of this type
    /// </summary>
    public RuntimeOptionsAdapter(ProgramArguments arguments)
    {
        _lazyOptions = new Lazy<RuntimeOptions>(() => BuildOptions(arguments));
    }

    /// <inheritdoc />
    public RuntimeOptions Value => _lazyOptions.Value;

    private RuntimeOptions BuildOptions(ProgramArguments arguments)
    {
        return new RuntimeOptions
        {
            CommandMode = arguments.Command,
            ConnectionOptions = BuildConnectionOptions(arguments),
            Output = BuildOutputOptions(arguments),
            DataSource = BuildDataSourceOptions(arguments),
            OutputPath = arguments.OutputPath,
            Parallelization = BuildParallelizationOptions(arguments),
            Clean = arguments.CleanMode != CleanConfirmMode.None
        };
    }

    private ParallelizationOptions BuildParallelizationOptions(ProgramArguments arguments)
    {
        return new ParallelizationOptions
        {
            BatchSize = arguments.BatchSize,
            MaxThreadCount = arguments.MaxThreads,
            OutputFlushInterval = TimeSpan.FromSeconds(arguments.OutputFlushIntervalSeconds)
        };
    }

    private static DataSourceOptions BuildDataSourceOptions(ProgramArguments arguments)
    {
        return new DataSourceOptions
        {
            Schema = arguments.SourceSchema,
            TableName = arguments.SourceTable,
            WatermarkColumnName = arguments.WatermarkColumnName,
            ExcludedColumns = arguments.ExcludedColumns.ToArray(),
            SelectColumns = arguments.SelectColumns.ToArray(),
            CommandRetryIntervals = arguments.CommandRetryIntervals
                .Select(seconds => TimeSpan.FromSeconds(seconds))
                .ToArray(),
            MaxRows = arguments.MaxRows
        };
    }

    private static OutputOptions BuildOutputOptions(ProgramArguments arguments)
    {
        return new OutputOptions
        {
            FileCompression = arguments.Compression,
            MaxFileSize = arguments.MaxFileSize,
            OutputFormat = arguments.OutputFormat
        };
    }

    private static ConnectionOptions BuildConnectionOptions(ProgramArguments arguments)
    {
        return new ConnectionOptions
        {
            Provider = arguments.DatabaseProvider,
            ConnectionString = arguments.ConnectionString,
            Server = arguments.Server,
            Database = arguments.Database,
            UserId = arguments.UserId,
            Password = arguments.Password,
            Properties = BuildConnectionProperties(arguments.ProviderProperties),
            ConnectRetryIntervals = arguments.ConnectRetryIntervals
                .Select(seconds => TimeSpan.FromSeconds(seconds))
                .ToArray()
        };
    }

    private static IReadOnlyDictionary<string, string> BuildConnectionProperties(Dictionary<string, string> properties)
    {
        return properties.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}