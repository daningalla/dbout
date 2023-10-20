using DbOut.Exceptions;
using DbOut.Options;

namespace DbOut.Providers;

public sealed class ProviderConnectionProperties
{
    private readonly ConnectionSpec _connectionSpec;
    private readonly IReadOnlyDictionary<string, string> _properties;

    public ProviderConnectionProperties(ConnectionSpec connectionSpec)
    {
        _connectionSpec = connectionSpec;
        _properties = _connectionSpec.Properties ?? throw ExceptionResources.NoConnectionProperties(connectionSpec);
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
            throw ExceptionResources.MissingRequiredConnectionProperty(_connectionSpec, key);
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
            throw ExceptionResources.InvalidConnectionPropertyConversion<T>(_connectionSpec, key, value);
        }
    }
}