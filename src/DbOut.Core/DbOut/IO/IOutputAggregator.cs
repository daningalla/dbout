using DbOut.Continuation;

namespace DbOut.IO;

public interface IOutputAggregator
{
    Task EnqueueCachedFileAsync(CacheFileDescriptor descriptor, bool isFromRestorePoint);
    Task FlushAsync(CancellationToken cancellationToken);
}