namespace DbOut.Metadata;

public record ColumnMetadata(
    string ColumnName,
    Type DataType,
    Type? AnnotatedDataType,
    bool IsNullable,
    int OrdinalPosition,
    ColumnIndexType IndexType,
    IValueConverter ValueConverter);