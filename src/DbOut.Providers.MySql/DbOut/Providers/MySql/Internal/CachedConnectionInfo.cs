using MySqlConnector;
using Polly.Retry;

namespace DbOut.Providers.MySql.Internal;

internal class CachedConnectionInfo
{
    public required string ConnectionString { get; init; }
    
    public required MySqlConnectionStringBuilder Builder { get; init; }
    
    public required AsyncRetryPolicy ConnectRetryPolicy { get; init; }
}