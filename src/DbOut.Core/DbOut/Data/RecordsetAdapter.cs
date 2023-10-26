using System.Data.Common;
using DbOut.Metadata;

namespace DbOut.Data;

public static class RecordsetAdapter
{
    public static async Task<Recordset> FillRecordsetAsync(
        DbDataReader dataReader, 
        ColumnSchema columnSchema,
        RecordsetBuffer recordsetBuffer,
        Action<DbDataReader>? sampleFunction,
        CancellationToken cancellationToken)
    {
        var rowsRead = 0;

        if (!await dataReader.ReadAsync(cancellationToken)) 
            return recordsetBuffer.CreateRecordset(columnSchema, rowsRead);
        
        sampleFunction?.Invoke(dataReader);
        
        var columnCount = dataReader.FieldCount;
        var array = recordsetBuffer.Array;
            
        do
        {
            for (var c = 0; c < columnCount; c++)
            {
                array[c, rowsRead] = dataReader.GetValue(c);
            }
                
            rowsRead++;
        } while (await dataReader.ReadAsync(cancellationToken));

        return recordsetBuffer.CreateRecordset(columnSchema, rowsRead);
    }
}