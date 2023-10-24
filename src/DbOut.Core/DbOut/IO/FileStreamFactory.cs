using System.IO.Compression;
using DbOut.Services;
using Microsoft.Extensions.Logging;

namespace DbOut.IO;

[Service]
public class FileStreamFactory
{
    private readonly ILogger<FileStreamFactory> _logger;

    /// <summary>
    /// Creates a new instance of this type
    /// </summary>
    public FileStreamFactory(ILogger<FileStreamFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Opens a read stream.
    /// </summary>
    /// <param name="path">Path</param>
    /// <param name="compression">Compression</param>
    /// <returns>Stream</returns>
    public Stream OpenReadStream(string path, FileCompression compression)
    {
        _logger.LogTrace("Creating read stream to {path}, compression={compression}", path, compression);

        var formattedPath = FormatPath(path, compression);
        
        return compression == FileCompression.None
            ? File.OpenRead(formattedPath)
            : new GZipStream(File.OpenRead(formattedPath), CompressionMode.Decompress);
    }

    /// <summary>
    /// Opens a write stream.
    /// </summary>
    /// <param name="path">Path</param>
    /// <param name="compression">Compression</param>
    /// <returns>Stream</returns>
    public Stream OpenWriteStream(string path, FileCompression compression)
    {
        var formattedPath = FormatPath(path, compression);
        
        _logger.LogTrace("Creating write stream to {path}, compression={compression}", path, compression);
        
        return compression == FileCompression.None
            ? File.OpenWrite(formattedPath)
            : new GZipStream(File.OpenWrite(formattedPath), CompressionMode.Compress);
    }

    /// <summary>
    /// Formats the file path.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="compression">Compression type.</param>
    /// <returns>Resolved path.</returns>
    public string FormatPath(string path, FileCompression compression)
    {
        return compression == FileCompression.GZip && Path.GetExtension(path) != ".gz" ? $"{path}.gz" : path;
    }
}