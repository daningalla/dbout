using DbOut.Options;

namespace DbOut.Exceptions;

internal static class ExceptionResources
{
    public static Exception NoConnectionProperties(ConnectionOptions connectionOptions)
    {
        return new ConfigurationException(string.Format(Resources.NoConnectionProperties, connectionOptions.Key));
    }

    public static Exception MissingRequiredConnectionProperty(ConnectionOptions connectionOptions, string key)
    {
        return new ConfigurationException(string.Format(Resources.MissingRequiredConnectionProperty, 
            connectionOptions.Key, key));
            
    }

    public static Exception InvalidConnectionPropertyConversion<T>(ConnectionOptions connectionOptions, string key, string value)
    {
        return new ConfigurationException(string.Format(Resources.InvalidConnectionPropertyConversion,
            connectionOptions.Key, key, typeof(T), value));
    }
}