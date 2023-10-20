using System.Data;
using DbOut.Options;
using MySqlConnector;

namespace DbOut.Providers.MySql;

/// <summary>
/// Provider for MySql.
/// </summary>
public sealed class MySqlDataProvider : IDataProvider
{
    /// <inheritdoc />
    public string Provider => typeof(MySqlConnection).FullName ?? nameof(MySqlConnection);

    /// <inheritdoc />
    public IDbConnection CreateConnection(ConnectionOptions connectionOptions)
    {
        return ConnectionBuilder.Create(connectionOptions);
    }
}