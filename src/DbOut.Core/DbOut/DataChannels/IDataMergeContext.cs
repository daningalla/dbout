namespace DbOut.DataChannels;

public interface IDataMergeContext
{
    Task<int> MergeFromStreamAsync(Stream stream, int index, int count, CancellationToken cancellationToken);
    Task<int> WriteToStreamAsync(Stream stream, CancellationToken cancellationToken);
}