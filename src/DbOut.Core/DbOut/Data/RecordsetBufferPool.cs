using Microsoft.Extensions.ObjectPool;

namespace DbOut.Data;

public sealed class RecordsetBufferPool
{
    private readonly ObjectPool<object?[,]> _objectPool;
    
    public RecordsetBufferPool(int columnCount, int rowCount, int capacity)
    {
        _objectPool = new DefaultObjectPool<object?[,]>(new RecordsetBufferPoolPolicy(columnCount, rowCount),
            capacity);
    }
    
    private sealed class RecordsetBufferPoolPolicy : PooledObjectPolicy<object?[,]>
    {
        private readonly int _columnCount;
        private readonly int _rowCount;

        internal RecordsetBufferPoolPolicy(int columnCount, int rowCount)
        {
            _columnCount = columnCount;
            _rowCount = rowCount;
        }

        /// <inheritdoc />
        public override object?[,] Create() => new object?[_columnCount, _rowCount];

        /// <inheritdoc />
        public override bool Return(object?[,] obj) => true;
    }

    public RecordsetBuffer GetInstance() => new(_objectPool);
}