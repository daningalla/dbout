using DbOut.Options;
using MySqlConnector;

namespace DbOut.Providers.MySql;

internal static class ConnectionBuilder
{
    internal static MySqlConnection Create(ConnectionSpec connectionSpec)
    {
        var connectionString = connectionSpec.ConnectionString ?? CreateConnectionString(connectionSpec);
        return new MySqlConnection(connectionString);
    }

    private static string CreateConnectionString(ConnectionSpec connectionSpec)
    {
        return connectionSpec.ConnectionString
               ?? CreateConnectionStringFromProperties(connectionSpec);
    }

    private static string CreateConnectionStringFromProperties(ConnectionSpec connectionSpec)
    {
        var properties = new ProviderConnectionProperties(connectionSpec);

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