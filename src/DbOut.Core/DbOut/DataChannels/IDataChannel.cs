using DbOut.Data;
using DbOut.Metadata;

namespace DbOut.DataChannels;

public interface IDataChannel
{
    string FormatType { get; }
    
    string CreateFileName(string file);
    
    Task WriteToStreamAsync(
        Stream stream,
        Recordset recordset,
        int rowIndex,
        int count,
        CancellationToken cancellationToken);

    IDataMergeContext CreateMergeContext(ColumnSchema columnSchema);
}