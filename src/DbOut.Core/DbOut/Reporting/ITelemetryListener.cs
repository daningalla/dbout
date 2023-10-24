namespace DbOut.Reporting;

public interface ITelemetryListener
{
    /// <summary>
    /// Increments a value.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="amount">Increment amount</param>
    void Increment(string key, long amount = 1);

    /// <summary>
    /// Tracks the minimum, maximum, and mean of the given value.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    void TrackNumericValue(string key, double value);

    /// <summary>
    /// Adds a list item.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    /// <typeparam name="T">Value type</typeparam>
    void PushListItem(string key, string value);

    void Write(TextWriter writer);

    /// <summary>
    /// Pushes a hash entry.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    void PushHashEntry(string key, string value);

    /// <summary>
    /// Gets a snapshot of the telemetry.
    /// </summary>
    /// <returns><see cref="SummaryTelemetryData"/></returns>
    SummaryTelemetryData GetSnapshot();

    /// <summary>
    /// Initializes telemetry values.
    /// </summary>
    /// <param name="telemetryData">Data</param>
    void InitializeWith(SummaryTelemetryData telemetryData);
}