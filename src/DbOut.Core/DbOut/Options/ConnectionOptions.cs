namespace DbOut.Options;

/// <summary>
/// Describes a connection.
/// </summary>
public class ConnectionOptions
{
    public required string Provider { get; init; }
    public required string Key { get; init; }
    public string? ConnectionString { get; init; }
    public required Dictionary<string, string>? Properties { get; init; }
}