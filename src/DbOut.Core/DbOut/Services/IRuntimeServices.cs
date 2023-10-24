using DbOut.DataChannels;
using DbOut.Providers;

namespace DbOut.Services;

/// <summary>
/// Manages a collection of services.
/// </summary>
public interface IRuntimeServices
{
    IDatabaseProvider DatabaseProvider { get; }
    
    IConnectionContext ConnectionContext { get; }
    
    IDataChannel DataChannel { get; }
}