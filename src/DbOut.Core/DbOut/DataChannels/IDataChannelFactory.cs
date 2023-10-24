using DbOut.Options;

namespace DbOut.DataChannels;

public interface IDataChannelFactory
{
    IDataChannel CreateChannel(OutputFormat format);
}