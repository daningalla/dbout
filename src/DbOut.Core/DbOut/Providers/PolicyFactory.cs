using DbOut.Reporting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace DbOut.Providers;

public static class PolicyFactory
{
    private static readonly TimeSpan[] DefaultRetryIntervals = new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    };
    
    public static AsyncRetryPolicy CreateRetryPolicy<TException>(
        string provider,
        string action,
        ILogger logger,
        IEnumerable<TimeSpan>? retryIntervals = null,
        Action<Exception, int>? onRetry = null)
        where TException : Exception
    {
        var intervals = (retryIntervals ?? DefaultRetryIntervals).ToArray();
        
        return Policy
            .Handle<TException>()
            .WaitAndRetryAsync(retryIntervals ?? DefaultRetryIntervals, (
                exception,
                span,
                attempt,
                _) =>
            {
                logger.LogWarning(
                    "Database provider {provider} action {action} encountered handled exception {exceptionType} " +
                    "(attempt {attempt}/{totalCount})\nException: {message}\nRe-attempt in {span} seconds.",
                    provider,
                    action,
                    exception.GetType(),
                    attempt,
                    intervals.Length,
                    exception.Message,
                    (int)span.TotalSeconds);
                
                onRetry?.Invoke(exception, attempt);
            });
    }
}