using DbOut.DataChannels;
using DbOut.Options;
using DbOut.Providers;
using Microsoft.Extensions.Options;

namespace DbOut.Services;

[Service]
public class RuntimeServices : IRuntimeServices
{
    private readonly RuntimeOptions _options;
    private readonly Lazy<IDatabaseProvider> _lazyDatabaseProvider;
    private readonly Lazy<IConnectionContext> _lazyConnectionContext;
    private readonly Lazy<IDataChannel> _lazyDataChannel;

    public RuntimeServices(
        IOptions<RuntimeOptions> options,
        IEnumerable<IDatabaseProvider> databaseProviders,
        IDataChannelFactory dataChannelFactory)
    {
        _options = options.Value;
        _lazyDatabaseProvider = new Lazy<IDatabaseProvider>(() => ResolveDatabaseProvider(databaseProviders));
        _lazyConnectionContext = new Lazy<IConnectionContext>(() => DatabaseProvider.CreateConnectionContext(_options));
        _lazyDataChannel = new Lazy<IDataChannel>(() => CreateDataChannel(dataChannelFactory, options.Value));
    }

    /// <inheritdoc />
    public IDatabaseProvider DatabaseProvider => _lazyDatabaseProvider.Value;

    /// <inheritdoc />
    public IConnectionContext ConnectionContext => _lazyConnectionContext.Value;

    /// <inheritdoc />
    public IDataChannel DataChannel => _lazyDataChannel.Value;

    private IDatabaseProvider ResolveDatabaseProvider(IEnumerable<IDatabaseProvider> databaseProviders)
    {
        var providers = databaseProviders.ToArray();
        var providerId = RuntimeOptions.ThrowIfNullOrEmpty(
            "Options.Connection.Provider", 
            _options.ConnectionOptions?.Provider);

        var provider = providers.FirstOrDefault(provider => provider.ProviderId == providerId);

        if (provider != null)
        {
            return provider;
        }

        throw new ArgumentException(
            $"Database provider '{providerId}' is not available. The following drivers are installed: " +
            string.Join(',', providers.Select(p => p.ProviderId)));
    }

    private IDataChannel CreateDataChannel(IDataChannelFactory dataChannelFactory, RuntimeOptions options)
    {
        var format = RuntimeOptions.ThrowIfNullReference("Options.Output", options.Output).OutputFormat;
        var channel = dataChannelFactory.CreateChannel(format);
        return channel;
    }
}