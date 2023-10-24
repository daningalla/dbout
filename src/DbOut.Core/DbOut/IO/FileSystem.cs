using System.Diagnostics;
using DbOut.Options;
using DbOut.Reporting;
using DbOut.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbOut.IO;

/// <summary>
/// Abstracts the file system.
/// </summary>
[Service]
public class FileSystem : IFileSystem
{
    private readonly ITelemetryListener _telemetryListener;
    private readonly ILogger<FileSystem> _logger;
    private readonly FileStreamFactory _fileStreamFactory;

    /// <summary>
    /// Creates a new instance of this type
    /// </summary>
    public FileSystem(
        ILogger<FileSystem> logger,
        FileStreamFactory fileStreamFactory,
        IOptions<RuntimeOptions> options,
        ITelemetryListener telemetryListener)
        : this(
            logger,
            fileStreamFactory,
            RuntimeOptions.ThrowIfNullOrEmpty("Options.OutputPath", options.Value.OutputPath),
            RuntimeOptions.ThrowIfNullReference("Options.Output", options.Value.Output).FileCompression,
            telemetryListener
        )
    {
    }
    
    private FileSystem(
        ILogger<FileSystem> logger, 
        FileStreamFactory fileStreamFactory,
        string basePath,
        FileCompression compression,
        ITelemetryListener telemetryListener,
        IFileSystem? parent = null)
    {
        BasePath = Path.GetFullPath(basePath);
        Compression = compression;
        Parent = parent;
        _logger = logger;
        _fileStreamFactory = fileStreamFactory;
        _telemetryListener = telemetryListener;

        var compressionString = Compression == FileCompression.None ? "UnCompressed" : "GZipCompressed";
        telemetryListener.PushHashEntry($"Services.{compressionString}FileSystem", nameof(FileSystem));
    }

    /// <summary>
    /// Gets the base path
    /// </summary>
    public string BasePath { get; }

    /// <inheritdoc />
    public bool DeleteFile(string filePath)
    {
        var formattedPath = _fileStreamFactory.FormatPath(
            MakePath(filePath), 
            Compression);

        if (!File.Exists(formattedPath))
            return false;
        
        File.Delete(formattedPath);
        _logger.LogTrace("Deleted file {path}", formattedPath);
        _telemetryListener.Increment("FileSystem.FilesDeleted");
        return true;
    }

    /// <summary>
    /// Gets the default compression mode.
    /// </summary>
    public FileCompression Compression { get; }

    /// <summary>
    /// Gets the parent file system.
    /// </summary>
    public IFileSystem? Parent { get; }

    /// <summary>
    /// Gets the root file system.
    /// </summary>
    public IFileSystem Root => Parent?.Root ?? this;

    /// <summary>
    /// Creates the directory.
    /// </summary>
    public void CreateDirectory()
    {
        if (Directory.Exists(BasePath)) return;
        
        Directory.CreateDirectory(BasePath);
        _logger.LogInformation("Created directory {path}", BasePath);
    }

    /// <inheritdoc />
    public FileInfo? GetFileInfo(string filePath)
    {
        var formattedPath = _fileStreamFactory.FormatPath(
            MakePath(filePath), 
            Compression);

        return File.Exists(formattedPath)
            ? new FileInfo(formattedPath)
            : null;
    }

    /// <summary>
    /// Opens a stream for reading.
    /// </summary>
    /// <param name="filePath">Path</param>
    /// <param name="asyncReader">Delegate that reads the stream.</param>
    /// <param name="compression">Compression level</param>
    /// <returns>Stream</returns>
    public async Task<T> ReadFromStreamAsync<T>(
        string filePath,
        Func<Stream, Task<T>> asyncReader,
        FileCompression? compression = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var formattedPath = _fileStreamFactory.FormatPath(
            MakePath(filePath), 
            compression ?? Compression);
        
        await using var stream = _fileStreamFactory.OpenReadStream(MakePath(filePath), 
            compression ?? Compression);
        
        _logger.LogTrace("Opened read stream to {path}", formattedPath);
        _telemetryListener.Increment("FileSystem.ReadStreamsOpened");
        
        var result = await asyncReader(stream);
        _telemetryListener.TrackNumericValue("Performance.OpenStreamReadTime (ms)", stopwatch.ElapsedMilliseconds);

        return result;
    }

    /// <summary>
    /// Opens a stream for writing.
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="state">State to pass to <paramref name="asyncWriter"/></param>
    /// <param name="asyncWriter">Delegate that writes to the stream</param>
    /// <typeparam name="TState">State type</typeparam>
    /// <returns>File info</returns>
    public async Task<FileInfo> WriteToStreamAsync<TState>(
        string filePath,
        TState state,
        Func<Stream, TState, Task> asyncWriter)
    {
        var stopwatch = Stopwatch.StartNew();
        EnsurePathCreated();
        var formattedPath = _fileStreamFactory.FormatPath(MakePath(filePath), Compression);
        {
            await using var stream = _fileStreamFactory.OpenWriteStream(
                MakePath(filePath), 
                Compression);

            _logger.LogTrace("Opened write stream to {path}", formattedPath);
            _telemetryListener.Increment("FileStream.WriteStreamsOpened");
            
            await asyncWriter(stream, state);
        }

        _telemetryListener.TrackNumericValue("Performance.OpenStreamWriteTime (ms)", stopwatch.ElapsedMilliseconds);
        return new FileInfo(formattedPath);
    }

    /// <summary>
    /// Makes a child file system that is a sub-directory in the current file system.
    /// </summary>
    /// <param name="childPath">Child path</param>
    /// <param name="defaultCompression">Default compression</param>
    /// <returns><see cref="IFileSystem"/></returns>
    public IFileSystem CreateForChildPath(string childPath, FileCompression defaultCompression)
    {
        return new FileSystem(_logger, 
            _fileStreamFactory, 
            Path.Combine(BasePath, childPath), 
            defaultCompression,
            _telemetryListener,
            this);
    }

    /// <inheritdoc />
    public void EnsurePathCreated()
    {
        if (Directory.Exists(BasePath))
            return;

        Directory.CreateDirectory(BasePath);
        _logger.LogDebug("Created directory {path}", BasePath);
    }

    /// <inheritdoc />
    public override string ToString() => BasePath;

    private string MakePath(string path)
    {
        return Path.Combine(BasePath, path);
    }
}