using Microsoft.IO;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace DbOut.DataChannels;

public class ParquetDataMergeContext : IDataMergeContext
{
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
    private readonly ParquetSchema _parquetSchema;
    private readonly List<Array[]> _cacheChunks = new(32);
    
    public ParquetDataMergeContext(ParquetSchema parquetSchema)
    {
        _parquetSchema = parquetSchema;
    }

    public async Task<int> MergeFromStreamAsync(
        Stream stream,
        int index,
        int count,
        CancellationToken cancellationToken)
    {
        // Buffer in case compressed
        using var memoryStream = MemoryStreamManager.GetStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        
        using var parquetStream = await ParquetReader.CreateAsync(memoryStream, cancellationToken: cancellationToken);
        using var groupReader = parquetStream.OpenRowGroupReader(0);
        var columnArray = new Array[_parquetSchema.DataFields.Length];
        var length = 0;

        for (var c = 0; c < _parquetSchema.DataFields.Length; c++)
        {
            var dataField = _parquetSchema.DataFields[c];
            var dataColumn = await groupReader.ReadColumnAsync(dataField, cancellationToken);
            var array = dataColumn.Data;
            columnArray[c] = CreateArraySubset(dataField, array, index, count);
            length = array.Length;
        }

        _cacheChunks.Add(columnArray);
        return length;
    }

    public async Task<int> WriteToStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var parquetStream = await ParquetWriter.CreateAsync(_parquetSchema, stream, 
            cancellationToken: cancellationToken);
        using var groupWriter = parquetStream.CreateRowGroup();
        var count = 0;

        for (var c = 0; c < _parquetSchema.DataFields.Length; c++)
        {
            var field = _parquetSchema.DataFields[c];
            var rowCount = _cacheChunks.Sum(row => row[c].Length);
            var array = CreateArrayWithFieldElementType(field, rowCount);
            var index = 0;
            foreach (var row in _cacheChunks)
            {
                var source = row[c];
                Array.Copy(source, 0, array, index, source.Length);
                index += source.Length;
                
                // free memory
                row[c] = Array.Empty<object?>();
            }

            var dataColumn = new DataColumn(field, array);
            await groupWriter.WriteColumnAsync(dataColumn, cancellationToken);

            if (c == 0) count += array.Length;
        }

        return count;
    }

    private static Array CreateArraySubset(DataField dataField, Array source, int index, int count)
    {
        if (index == 0 && source.Length == count)
            return source;

        var newArray = CreateArrayWithFieldElementType(dataField, count);
        Array.Copy(source, index, newArray, 0, count);
        return newArray;
    }

    private static Array CreateArrayWithFieldElementType(DataField field, int length)
    {
        var elementType = field.ClrType switch
        {
            { IsClass: false } when field.IsNullable => typeof(Nullable<>).MakeGenericType(field.ClrType),
            _ => field.ClrType
        };

        return Array.CreateInstance(elementType, length);
    }
}