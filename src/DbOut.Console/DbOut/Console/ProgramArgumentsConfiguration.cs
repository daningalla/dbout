using DbOut.IO;
using DbOut.Options;
using Microsoft.Extensions.Logging;
using Vertical.CommandLine.Configuration;

namespace DbOut.Console;

public class ProgramArgumentsConfiguration : ApplicationConfiguration<ProgramArguments>
{
    public ProgramArgumentsConfiguration()
    {
        Option<string?>("-p|--provider", arg => arg.Map.ToProperty(opt => opt.DatabaseProvider));
        Option<string?>("-c|--connection-string", arg => arg.Map.ToProperty(opt => opt.ConnectionString));
        Option<string?>("-s|--server", arg => arg.Map.ToProperty(opt => opt.Server));
        Option<string?>("-d|--database", arg => arg.Map.ToProperty(opt => opt.Database));
        Option<string?>("-u|--user", arg => arg.Map.ToProperty(opt => opt.UserId));
        Option<string?>("--password", arg => arg.Map.ToProperty(opt => opt.Password));
        Option<string>("--provider-prop", arg => arg.Map.Using(MapConnectionProperty));
        Option<LogLevel>("--log-level", arg => arg.Map.ToProperty(opt => opt.LogLevel));
        Option<string?>("--file-log", arg => arg.Map.ToProperty(opt => opt.FileLogPath));
        Option<string>("--file-log-size", arg => arg.Map.Using((opt, value) => 
            opt.MaxLogFileSize = MapFileSize("--file-log-size", value)));
        Option("--connect-retry-intervals", arg => arg.Map.Using((opt, value) => MapRetryIntervals(
            opt.ConnectRetryIntervals,
            "--connect-retry-intervals",
            value)));
        Switch("--list-providers", arg => arg.Map.Using((opt, _) => opt.Command = CommandMode.ListProviders));
        Switch("--get-schema", arg => arg.Map.Using((opt, _) => opt.Command = CommandMode.GetSchema));
        Switch("--validate-connection", arg => arg.Map.Using((opt, _) => opt.Command = CommandMode.ValidateConnection));
        Option<string?>("--out", arg => arg.Map.ToProperty(opt => opt.OutputPath));
        Option<string>("--max-file-size", arg => arg.Map.Using((opt, value) => opt.MaxFileSize = MapFileSize(
            "--max-file-size", value)));
        Option<FileCompression>("--compression", arg => arg.Map.ToProperty(opt => opt.Compression));
        Option<string?>("--schema", arg => arg.Map.ToProperty(opt => opt.SourceSchema));
        Option<string?>("--table", arg => arg.Map.ToProperty(opt => opt.SourceTable));
        Option<string?>("--watermark", arg => arg.Map.ToProperty(opt => opt.WatermarkColumnName));
        Option<int>("--batch-size", arg => arg.Map.ToProperty(opt => opt.BatchSize).Validate.Greater(0));
        Option<int>("--threads", arg => arg.Map.ToProperty(opt => opt.MaxThreads).Validate.Greater(0));
        Option<string>("--select-columns", arg => arg.Map.Using((opt, value) => MapColumnSet(opt.SelectColumns, value)));
        Option<string>("--exclude-columns", arg => arg.Map.Using((opt, value) => MapColumnSet(opt.ExcludedColumns, value)));
        Option<string>("--command-retry-intervals", arg => arg.Map.Using((opt, value) => MapRetryIntervals(
            opt.CommandRetryIntervals,
            "--command-retry-intervals",
            value)));
        Option<int>("--max-rows", arg => arg.Map.ToProperty(opt => opt.MaxRows).Validate.Greater(0));
        Option<OutputFormat>("--out-format", arg => arg.Map.ToProperty(opt => opt.OutputFormat));
        Option<int>("--out-flush-interval", arg => arg.Map.ToProperty(opt => opt.OutputFlushIntervalSeconds));
        Option<CleanConfirmMode>("--clean", arg => arg.Map.ToProperty(opt => opt.CleanMode));
    }

    private static void MapColumnSet(HashSet<string> set, string value)
    {
        foreach (var columnName in value.Split(
                     ',',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            set.Add(columnName);
        }
    }

    private static FileSize MapFileSize(string argTemplate, string value)
    {
        if (!FileSize.TryParse(value, out var fileSize))
        {
            throw ExceptionFormatter.InvalidFileSize(argTemplate, value);
        }

        return fileSize;
    }

    private static void MapRetryIntervals(ICollection<int> list, string argTemplate, string value)
    {
        try
        {
            foreach (var strValue in value.Split(','))
            {
                var intValue = int.Parse(strValue);
                if (intValue < 0)
                    throw new ArgumentException();
                
                list.Add(int.Parse(strValue));
            }
        }
        catch (Exception)
        {
            throw ExceptionFormatter.InvalidRetryInterval(argTemplate, value);
        }
    }

    private static void MapConnectionProperty(ProgramArguments opt, string value)
    {
        var split = value.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2)
        {
            throw ExceptionFormatter.InvalidAdHocConnectionProperty(value);
        }

        opt.ProviderProperties[split[0]] = split[1];
    }
}