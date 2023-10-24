namespace DbOut.Data;

public interface IPartitionedQueryEngine
{
    Task<QueryEngineExitState> ExecuteAsync(
        BatchingParameters parameters,
        CancellationToken cancellationToken);
}