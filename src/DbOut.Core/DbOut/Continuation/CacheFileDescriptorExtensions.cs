namespace DbOut.Continuation;

public static class CacheFileDescriptorExtensions
{
    public static (CacheFileDescriptor, CacheFileDescriptor) Split(this CacheFileDescriptor descriptor, int count)
    {
        if (count < 1)
        {
            throw new ArgumentException("Count less than 1");
        }
        
        var first = descriptor.ToSubset(descriptor.SubsetId + 1, 0, count);
        var second = descriptor.ToSubset(descriptor.SubsetId + 2, count, descriptor.ActualCount - count);

        return (first, second);
    }
    
    public static CacheFileDescriptor ToSubset(
        this CacheFileDescriptor descriptor,
        int subsetId,
        int offset,
        int count)
    {
        return new CacheFileDescriptor
        {
            Id = Guid.NewGuid().ToString(),
            QueryHash = descriptor.QueryHash,
            FilePath = descriptor.FilePath,
            FileType = descriptor.FileType,
            SubsetId = subsetId,
            CompressionType = descriptor.CompressionType,
            QueryCount = descriptor.QueryCount,
            ContentHash = descriptor.ContentHash,
            SizeInBytes = descriptor.SizeInBytes,
            CommitOffset = descriptor.CommitOffset + offset,
            MergeOffset = descriptor.MergeOffset + offset,
            ActualCount = count
        };
    }
}