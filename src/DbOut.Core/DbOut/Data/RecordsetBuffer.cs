using DbOut.Metadata;
using Microsoft.Extensions.ObjectPool;

namespace DbOut.Data;

public sealed class RecordsetBuffer : IDisposable
{
    private readonly ObjectPool<object?[,]> _objectPool;

    public RecordsetBuffer(ObjectPool<object?[,]> objectPool)
    {
        _objectPool = objectPool;
        Array = objectPool.Get();
    }
    
    public object?[,] Array { get; }

    public Recordset CreateRecordset(ColumnSchema columnSchema, int rowCount)
    {
        return new Recordset(columnSchema, this, rowCount);
    }

    public void Dispose() => _objectPool.Return(Array);
}