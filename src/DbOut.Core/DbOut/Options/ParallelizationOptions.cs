namespace DbOut.Options;

public class ParallelizationOptions
{
    /// <summary>
    /// Gets the batch size.
    /// </summary>
    public required int BatchSize { get; init; }

    /// <summary>
    /// Gets the max number of concurrent query threads.
    /// </summary>
    public required int MaxThreadCount { get; init; } = 5;

    /// <summary>
    /// Gets the number of seconds between each attempt to flush cached data to output files.
    /// </summary>
    public required TimeSpan OutputFlushInterval { get; init; }
}