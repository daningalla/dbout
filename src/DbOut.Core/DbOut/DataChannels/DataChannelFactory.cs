using DbOut.Options;
using DbOut.Reporting;
using DbOut.Services;
using Microsoft.Extensions.Logging;

namespace DbOut.DataChannels;

[Service]
public class DataChannelFactory : IDataChannelFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ITelemetryListener _telemetryListener;
    private readonly ILogger<DataChannelFactory> _logger;

    public DataChannelFactory(ILoggerFactory loggerFactory, ITelemetryListener telemetryListener)
    {
        _loggerFactory = loggerFactory;
        _telemetryListener = telemetryListener;
        _logger = loggerFactory.CreateLogger<DataChannelFactory>();
    }
    
    /// <inheritdoc />
    public IDataChannel CreateChannel(OutputFormat format)
    {
        var channel = format switch
        {
            OutputFormat.Parquet => new ParquetDataChannel(_loggerFactory.CreateLogger<ParquetDataChannel>(),
                _telemetryListener),
            _ => throw new NotImplementedException()
        };
        
        _logger.LogDebug("Created data channel of type {type}", channel.GetType());
        
        return channel;
    }
}