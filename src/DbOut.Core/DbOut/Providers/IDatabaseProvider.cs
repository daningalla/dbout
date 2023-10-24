using DbOut.Options;

namespace DbOut.Providers;

/// <summary>
/// Interface for data providers to implement.
/// </summary>
public interface IDatabaseProvider
{
    string ProviderId { get; }

    IConnectionContext CreateConnectionContext(RuntimeOptions options);
}