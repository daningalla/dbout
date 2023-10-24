using DbOut.Continuation;
using DbOut.IO;

namespace DbOut.UnitTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var src = new CacheFileDescriptor
        {
            Id = Guid.NewGuid().ToString(),
            MergeOffset = 0,
            CommitOffset = 0,
            ActualCount = 1000,
            CompressionType = FileCompression.None,
            ContentHash = Guid.NewGuid().ToString("N"),
            FilePath = Path.GetTempFileName(),
            FileType = "json",
            QueryCount = 2500,
            QueryHash = Guid.NewGuid().ToString("N"),
            SubsetId = 0,
            SizeInBytes = 500000
        };

        var (merge, defer) = src.Split(500);

        for (;;)
        {
            (merge, defer) = defer.Split(defer.ActualCount / 2);
        }
    }
}