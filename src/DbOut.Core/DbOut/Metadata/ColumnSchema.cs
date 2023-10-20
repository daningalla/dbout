namespace DbOut.Metadata;

/// <summary>
/// Defines a column schema.
/// </summary>
public class ColumnSchema
{
    /// <summary>
    /// Gets the column name.
    /// </summary>
    public required string ColumnName { get; init; }
    
    /// <summary>
    /// Gets the ordinal position.
    /// </summary>
    public required int OrdinalPosition { get; init; }
    
    /// <summary>
    /// Gets the data type.
    /// </summary>
    public required Type DataType { get; init; }
    
    /// <summary>
    /// Gets the type with null annotation (struct types only).
    /// </summary>
    public required Type? NullAnnotatedDataType { get; init; }
    
    /// <summary>
    /// Gets the column key type.
    /// </summary>
    public required ColumnKeyType KeyType { get; init; }
    
    /// <summary>
    /// Gets whether the column is nullable.
    /// </summary>
    public required bool IsNullable { get; init; }
    
    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    public required int? MaximumLength { get; init; }

    public override string ToString() => $"{ColumnName}:{DataType}{LengthDisplay}{NullableDisplay}";

    private string LengthDisplay => MaximumLength.HasValue ? $"({MaximumLength}) " : string.Empty;
    private string NullableDisplay => IsNullable ? "nullable" : "not nullable";
}