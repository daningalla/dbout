using DbOut.IO;

namespace DbOut.Continuation;

public class CacheFileDescriptor
{
    public required string Id { get; init; }
    public required string QueryHash { get; init; }
    public required string FilePath { get; init; }
    public required string FileType { get; init; }
    public required int SubsetId { get; init; }
    public required FileCompression CompressionType { get; init; }
    public required int QueryCount { get; init; }
    public required int ActualCount { get; init; }
    public required int CommitOffset { get; init; }
    public required int MergeOffset { get; init; }
    public required string ContentHash { get; init; }
    public required long SizeInBytes { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return ActualCount switch
        {
            0 => "[]",
            1 => $"[{CommitOffset}]",
            _ => $"[{CommitOffset}..{CommitOffset+ActualCount}]"
        };
    }
} 