namespace DbOut.Metadata;

/// <summary>
/// Converts value types.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    /// Gets the value type.
    /// </summary>
    Type ValueType { get; }
    
    /// <summary>
    /// Gets the annotated value type.
    /// </summary>
    Type? AnnotatedValueType { get; }
    
    /// <summary>
    /// Gets the default value for the converter type.
    /// </summary>
    object DefaultValue { get; }
    
    /// <summary>
    /// Creates an array.
    /// </summary>
    /// <param name="length">Length</param>
    /// <returns>Array</returns>
    Array CreateArray(int length);

    /// <summary>
    /// Converts an object value.
    /// </summary>
    /// <param name="source">Source value</param>
    /// <returns>Converted value</returns>
    object? ConvertToValueType(object? source);

    /// <summary>
    /// Converts a string value to the source type.
    /// </summary>
    /// <param name="str">String</param>
    /// <returns>Converted value</returns>
    object ConvertToSourceType(string str);
}