using DbOut.Options;
using MySqlConnector;

namespace DbOut.Providers.MySql;

internal static class ConnectionBuilder
{
    internal static MySqlConnection Create(ConnectionOptions connectionOptions)
    {
        var connectionString = connectionOptions.ConnectionString ?? CreateConnectionString(connectionOptions);
        return new MySqlConnection(connectionString);
    }

    private static string CreateConnectionString(ConnectionOptions connectionOptions)
    {
        return connectionOptions.ConnectionString
               ?? CreateConnectionStringFromProperties(connectionOptions);
    }

    private static string CreateConnectionStringFromProperties(ConnectionOptions connectionOptions)
    {
        var properties = new ProviderConnectionProperties(connectionOptions);

        return new MySqlConnectionStringBuilder
        {
            Server = properties.GetProperty(nameof(MySqlConnectionStringBuilder.Server), required: true),
            Database = properties.GetProperty(nameof(MySqlConnectionStringBuilder.Database), required: true),
            UserID = properties.GetProperty(nameof(MySqlConnectionStringBuilder.UserID), required: true),
            Password = properties.GetProperty(nameof(MySqlConnectionStringBuilder.Password), required: true),
            DefaultCommandTimeout = properties.GetProperty(nameof(MySqlConnectionStringBuilder.DefaultCommandTimeout),
                converter: uint.Parse,
                defaultValue: 60U),
            ConnectionTimeout = properties.GetProperty(nameof(MySqlConnectionStringBuilder.ConnectionTimeout),
                converter: uint.Parse,
                defaultValue: 60U)
        }.ToString();
    }
}