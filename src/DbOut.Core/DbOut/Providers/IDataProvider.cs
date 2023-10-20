using System.Data;
using DbOut.Options;

namespace DbOut.Providers;

/// <summary>
/// Represents a data provider.
/// </summary>
public interface IDataProvider
{
    string Provider { get; }

    IDbConnection CreateConnection(ConnectionSpec connectionSpec);
}