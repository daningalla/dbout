using DbOut.IO;
using DbOut.Options;
using Microsoft.Extensions.Logging;

namespace DbOut.Console;

/// <summary>
/// Defines the program arguments.
/// </summary>
public class ProgramArguments
{
    public string? DatabaseProvider { get; set; }
    public string? ConnectionString { get; set; }
    public string? Server { get; set; }
    public string? Database { get; set; }
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public Dictionary<string, string> ProviderProperties { get; } = new();
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public string? FileLogPath { get; set; }
    public FileSize MaxLogFileSize { get; set; } = new(FileSizeUnit.Megabyte, 10);
    public CommandMode Command { get; set; } = CommandMode.Execute;
    public List<int> ConnectRetryIntervals { get; } = new();
    public string? OutputPath { get; set; }
    public FileCompression Compression { get; set; } = FileCompression.GZip;
    public FileSize MaxFileSize { get; set; } = new FileSize(FileSizeUnit.Megabyte, 25);
    public string? SourceSchema { get; set; }
    public string? SourceTable { get; set; }
    public string? WatermarkColumnName { get; set; }
    public int BatchSize { get; set; } = 2500;
    public int MaxThreads { get; set; } = 5;
    public int MaxRows { get; set; } = int.MaxValue;
    public HashSet<string> SelectColumns { get; } = new();
    public HashSet<string> ExcludedColumns { get; } = new();
    public List<int> CommandRetryIntervals { get; } = new();
    public int OutputFlushIntervalSeconds { get; set; } = 5;
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Parquet;
    public CleanConfirmMode CleanMode { get; set; }
}