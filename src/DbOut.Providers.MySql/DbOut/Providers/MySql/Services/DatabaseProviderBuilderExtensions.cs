using DbOut.Services;

namespace DbOut.Providers.MySql.Services;

public static class DatabaseProviderBuilderExtensions
{
    public static DatabaseProviderBuilder AddMySql(this DatabaseProviderBuilder builder)
    {
        builder.Services.AddServicesFromAssembly(typeof(DatabaseProviderBuilderExtensions).Assembly);
        return builder;
    }
}