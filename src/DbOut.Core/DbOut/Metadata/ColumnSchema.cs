using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DbOut.Metadata;

/// <summary>
/// Represents the column schema for a table.
/// </summary>
public sealed class ColumnSchema : IEnumerable<ColumnMetadata>
{
    private readonly Dictionary<string, ColumnMetadata> _columnNameIndex;

    /// <summary>
    /// Creates a new instance of this type
    /// </summary>
    public ColumnSchema(
        string schemaName,
        string tableName,
        IEnumerable<ColumnMetadata> columnMetadata)
    {
        SchemaName = schemaName;
        TableName = tableName;
        Columns = columnMetadata.OrderBy(c => c.OrdinalPosition).ToArray();
        _columnNameIndex = Columns.ToDictionary(c => c.ColumnName);
    }

    /// <summary>
    /// Gets the schema.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Tries to get a column.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="columnMetadata">Metadata if the column name was found.</param>
    /// <returns><c>true</c> if the column was found.</returns>
    public bool TryGetColumn(string columnName, [NotNullWhen(true)] out ColumnMetadata? columnMetadata)
    {
        return _columnNameIndex.TryGetValue(columnName, out columnMetadata);
    }

    /// <summary>
    /// Gets the unique schema id.
    /// </summary>
    public Guid SchemaId { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets a column by index.
    /// </summary>
    /// <param name="index"></param>
    public ColumnMetadata this[int index] => Columns[index];

    /// <summary>
    /// Gets a column by name.
    /// </summary>
    /// <param name="columnName"></param>
    public ColumnMetadata this[string columnName] => _columnNameIndex[columnName];
    
    /// <summary>
    /// Gets the columns.
    /// </summary>
    public IReadOnlyList<ColumnMetadata> Columns { get; }

    /// <summary>
    /// Gets the column count.
    /// </summary>
    public int Count => Columns.Count();

    /// <inheritdoc />
    public IEnumerator<ColumnMetadata> GetEnumerator() => Columns.GetEnumerator();

    /// <inheritdoc />
    public override string ToString() => "count={Columns.Count}";

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}