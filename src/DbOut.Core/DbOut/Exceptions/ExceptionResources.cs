using DbOut.Options;

namespace DbOut.Exceptions;

internal static class ExceptionResources
{
    public static Exception NoConnectionProperties(ConnectionSpec connectionSpec)
    {
        return new ConfigurationException(string.Format(Resources.NoConnectionProperties, connectionSpec.Key));
    }

    public static Exception MissingRequiredConnectionProperty(ConnectionSpec connectionSpec, string key)
    {
        return new ConfigurationException(string.Format(Resources.MissingRequiredConnectionProperty, 
            connectionSpec.Key, key));
            
    }

    public static Exception InvalidConnectionPropertyConversion<T>(ConnectionSpec connectionSpec, string key, string value)
    {
        return new ConfigurationException(string.Format(Resources.InvalidConnectionPropertyConversion,
            connectionSpec.Key, key, typeof(T), value));
    }
}