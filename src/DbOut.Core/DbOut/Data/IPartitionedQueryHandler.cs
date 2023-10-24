namespace DbOut.Data;

public interface IPartitionedQueryHandler
{
    Task<int> QueryAsync(PartitionedQuery query, CancellationToken cancellationToken);
}