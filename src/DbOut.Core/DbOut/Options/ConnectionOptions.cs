using System.Text.Json.Serialization;

namespace DbOut.Options;

/// <summary>
/// Defines connection options.
/// </summary>
public class ConnectionOptions
{
    public required string? Provider { get; init; }
    
    [JsonIgnore]
    public string? ConnectionString { get; init; }
    
    public string? Server { get; init; }
    
    public string? Database { get; init; }
    
    [JsonIgnore]
    public string? UserId { get; init; }
    
    [JsonIgnore]
    public string? Password { get; init; }
    
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
    
    public TimeSpan[]? ConnectRetryIntervals { get; init; }
}