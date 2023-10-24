namespace DbOut.Metadata;

public static class ValueConverter
{
    public static IValueConverter Create<T>(bool isNullable, Func<string, T> stringConverter)
    {
        return Create<T, T>(isNullable, src => src, stringConverter);
    }
    
    public static IValueConverter Create<TSource, TDest>(bool isNullable, 
        Func<TSource, TDest> valueConverter,
        Func<string, TSource> stringConverter)
    {
        var type = typeof(TDest);
        var annotatedType = !type.IsClass && isNullable
            ? typeof(Nullable<>).MakeGenericType(type)
            : null;

        return new ValueConverter<TSource, TDest>
        {
            ValueType = type,
            AnnotatedValueType = annotatedType,
            ValueTypeConverter = valueConverter,
            StringConverter = stringConverter
        };
    }
}

public class ValueConverter<TSource, TDest> : IValueConverter
{
    /// <inheritdoc />
    public required Type ValueType { get; init; }

    /// <inheritdoc />
    public required Type? AnnotatedValueType { get; init; }

    /// <inheritdoc />
    public object DefaultValue => default(TDest) ?? throw new InvalidOperationException();

    /// <summary>
    /// Function that performs conversion.
    /// </summary>
    public required Func<TSource, TDest> ValueTypeConverter { get; init; }
    
    /// <summary>
    /// Function that performs conversion.
    /// </summary>
    public required Func<string, TSource> StringConverter { get; init; }

    /// <inheritdoc />
    public Array CreateArray(int length)
    {
        return Array.CreateInstance(AnnotatedValueType ?? ValueType, length);
    }

    /// <inheritdoc />
    public object? ConvertToValueType(object? source)
    {
        return source is null or DBNull
            ? default
            : ValueTypeConverter((TSource)source);
    }

    /// <inheritdoc />
    public object ConvertToSourceType(string str)
    {
        return StringConverter(str) ?? throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public override string ToString() => $"{typeof(TSource)} => {AnnotatedValueType ?? ValueType}";
}