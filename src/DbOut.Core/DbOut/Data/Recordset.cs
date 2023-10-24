using DbOut.Metadata;

namespace DbOut.Data;

public sealed class Recordset : IDisposable
{
    private readonly RecordsetBuffer _disposableBuffer;
    private readonly object?[,] _buffer;

    public Recordset(ColumnSchema columnSchema, RecordsetBuffer buffer, int rowCount)
    {
        _disposableBuffer = buffer;
        _buffer = buffer.Array;
        ColumnSchema = columnSchema;
        RowCount = rowCount;
    }

    public ColumnSchema ColumnSchema { get; }
    public int RowCount { get; }

    public void Dispose() => _disposableBuffer.Dispose();

    public Array GetColumnData(int columnIndex, int rowIndex, int count)
    {
        var columnMetadata = ColumnSchema[columnIndex];
        var converter = columnMetadata.ValueConverter;
        var array = converter.CreateArray(count);
        var upperBound = rowIndex + count;
        var insertIndex = 0;
        var data = _buffer;

        for (var r = rowIndex; r < upperBound; r++)
        {
            var source = data[columnIndex, r];
            var dest = converter.ConvertToValueType(source);
            array.SetValue(dest, insertIndex);
            insertIndex++;
        }

        return array;
    }
}