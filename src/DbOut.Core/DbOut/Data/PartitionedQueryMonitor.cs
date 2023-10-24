using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DbOut.Data;

public sealed class PartitionedQueryMonitor : IDisposable
{
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _innerCancellationSource = new();
    private readonly CancellationTokenSource _rootCancellationSource;
    private readonly ConcurrentBag<Exception> _exceptions = new();
    
    public PartitionedQueryMonitor(ILogger logger, CancellationToken outerCancellationToken)
    {
        _logger = logger;
        _rootCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
            outerCancellationToken,
            _innerCancellationSource.Token);   
        OuterCancellationToken = outerCancellationToken;
        GlobalCancellationToken = _rootCancellationSource.Token;
    }

    public CancellationToken OuterCancellationToken { get; }
    
    public CancellationToken GlobalCancellationToken { get; }

    public bool IsCancellationRequested => GlobalCancellationToken.IsCancellationRequested;

    public void ThrowIfFaulted()
    {
        var exceptions = _exceptions.ToArray();
        if (exceptions.Length == 0)
            return;
        throw new AggregateException(exceptions);
    }

    public void SignalComplete()
    {
        _logger.LogDebug("Query reader complete signaled.");
        _innerCancellationSource.Cancel();
    }

    public void SignalFault(Exception exception)
    {
        _logger.LogWarning("Query reader fault signaled.");
        _exceptions.Add(exception);
        _innerCancellationSource.Cancel();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _innerCancellationSource.Dispose();
        _rootCancellationSource.Dispose();
    }
}