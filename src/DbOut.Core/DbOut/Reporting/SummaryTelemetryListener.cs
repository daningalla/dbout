using DbOut.Services;
using DbOut.Threading;

namespace DbOut.Reporting;

[Service]
public class SummaryTelemetryListener : ITelemetryListener
{
    private readonly VolatileValue<SummaryTelemetryData> _telemetryData = new(new SummaryTelemetryData());
    
    /// <summary>
    /// Increments a value.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="amount">Increment amount</param>
    public void Increment(string key, long amount = 1)
    {
        _telemetryData.Read(data =>
        {
            var value = data.Counters.GetValueOrDefault(key);
            data.Counters[key] = value + amount;
        });
    }

    /// <summary>
    /// Tracks the minimum, maximum, and mean of the given value.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    public void TrackNumericValue(string key, double value)
    {
        _telemetryData.Read(data =>
        {
            var entry = data.Numerics.GetValueOrDefault(key);
            if (entry == null)
            {
                data.Numerics.Add(key, entry = new TrackedNumeric
                {
                    Value = value,
                    Min = value,
                    Max = value,
                    MeanCount = 1,
                    MeanTotal = value
                });
            }
        
            entry.Update(value);
        });
    }

    /// <summary>
    /// Adds a list item.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    public void PushListItem(string key, string value)
    {
        _telemetryData.Read(data => data.Lists.Add(new KeyValuePair<string, string>(key, value)));
    }

    /// <summary>
    /// Pushes a hash entry.
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    public void PushHashEntry(string key, string value)
    {
        _telemetryData.Read(data => data.HashSets.Add(new KeyValuePair<string, string>(key, value)));
    }

    /// <inheritdoc />
    public SummaryTelemetryData GetSnapshot()
    {
        var copy = new SummaryTelemetryData();
        _telemetryData.Read(data =>
        {
            foreach (var (key, value) in data.Counters)
                copy.Counters.Add(key, value);
            foreach (var (key, value) in data.Numerics)
                copy.Numerics.Add(key, value.Copy());
            foreach (var entry in data.Lists)
                copy.Lists.Add(entry);
            foreach (var entry in data.HashSets)
                copy.HashSets.Add(entry);
        });
        return copy;
    }

    /// <inheritdoc />
    public void InitializeWith(SummaryTelemetryData telemetryData)
    {
        _telemetryData.Exchange(_ => telemetryData);
    }

    public void Write(TextWriter writer)
    {
        _telemetryData.Read(data => SummaryTelemetryWriter.Write(data, writer));
    }
}