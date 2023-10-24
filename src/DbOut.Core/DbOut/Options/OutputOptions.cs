using DbOut.IO;

namespace DbOut.Options;

public class OutputOptions
{
    public FileCompression FileCompression { get; init; }
    
    public FileSize? MaxFileSize { get; init; }
    
    public OutputFormat OutputFormat { get; init; }
}