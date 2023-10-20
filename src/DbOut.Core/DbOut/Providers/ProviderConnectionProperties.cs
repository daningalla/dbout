using DbOut.Exceptions;
using DbOut.Options;

namespace DbOut.Providers;

public sealed class ProviderConnectionProperties
{
    private readonly ConnectionOptions _connectionOptions;
    private readonly IReadOnlyDictionary<string, string> _properties;

    public ProviderConnectionProperties(ConnectionOptions connectionOptions)
    {
        _connectionOptions = connectionOptions;
        _properties = _connectionOptions.Properties ?? throw ExceptionResources.NoConnectionProperties(connectionOptions);
    }

    public string GetProperty(string key, bool required = false) => GetProperty(key, required, str => str)!;
    
    public T? GetProperty<T>(string key, bool required = false, Func<string, T>? converter = null,
        T? defaultValue = default)
    {
        var value = _properties.GetValueOrDefault(key);
        
        if (!string.IsNullOrWhiteSpace(value))
            return converter != null
                ? ConvertValue(key, value, converter)
                : (T)(object)value;
        
        if (required)
        {
            throw ExceptionResources.MissingRequiredConnectionProperty(_connectionOptions, key);
        }

        return defaultValue;
    }

    private T ConvertValue<T>(string key, string value, Func<string, T> converter)
    {
        try
        {
            return converter(value);
        }
        catch (Exception)
        {
            throw ExceptionResources.InvalidConnectionPropertyConversion<T>(_connectionOptions, key, value);
        }
    }
}