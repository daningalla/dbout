namespace DbOut.Reporting;

/// <summary>
/// Repository for summary telemetry.
/// </summary>
public sealed class SummaryTelemetryData
{
    public Dictionary<string, long> Counters { get; } = new(64);
    public Dictionary<string, TrackedNumeric> Numerics { get; } = new(64);
    public List<KeyValuePair<string, string>> Lists { get; } = new(32);
    public HashSet<KeyValuePair<string, string>> HashSets { get; } = new(32);
}