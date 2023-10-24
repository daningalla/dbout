using System.Text.Json.Serialization;

namespace DbOut.Options;

public class DataSourceOptions
{
    /// <summary>
    /// Gets the schema.
    /// </summary>
    public required string? Schema { get; init; }
    
    /// <summary>
    /// Gets the table name.
    /// </summary>
    public required string? TableName { get; init; }
    
    /// <summary>
    /// Gets the watermark column name.
    /// </summary>
    public required string? WatermarkColumnName { get; init; }
    
    /// <summary>
    /// Gets the select columns.
    /// </summary>
    public string[]? SelectColumns { get; init; }
    
    /// <summary>
    /// Gets the excluded columns.
    /// </summary>
    public string[]? ExcludedColumns { get; init; }
    
    /// <summary>
    /// Gets the command retry intervals.
    /// </summary>
    public TimeSpan[]? CommandRetryIntervals { get; init; }
    
    /// <summary>
    /// Gets the max row count.;
    /// </summary>
    public int MaxRows { get; init; }
}