namespace DbOut.IO;

/// <summary>
/// Abstracts the file system.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gets the base path
    /// </summary>
    string BasePath { get; }

    /// <summary>
    /// Gets the parent file system.
    /// </summary>
    IFileSystem? Parent { get; }

    /// <summary>
    /// Gets the root entry.
    /// </summary>
    IFileSystem Root { get; }

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    bool DeleteFile(string filePath);

    /// <summary>
    /// Gets file info.
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>FileInfo if the file exists.</returns>
    FileInfo? GetFileInfo(string filePath);

    /// <summary>
    /// Gets the default compression mode.
    /// </summary>
    FileCompression Compression { get; }

    /// <summary>
    /// Opens a stream for reading.
    /// </summary>
    /// <param name="filePath">Path</param>
    /// <param name="asyncReader">Delegate that reads the stream.</param>
    /// <param name="compression">Compression level</param>
    /// <returns>Stream</returns>
    Task<T> ReadFromStreamAsync<T>(
        string filePath,
        Func<Stream, Task<T>> asyncReader,
        FileCompression? compression = null);

    /// <summary>
    /// Opens a stream for writing.
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="state">State to pass to <paramref name="asyncWriter"/></param>
    /// <param name="asyncWriter">Delegate that writes to the stream</param>
    /// <typeparam name="TState">State type</typeparam>
    /// <returns>File info</returns>
    Task<FileInfo> WriteToStreamAsync<TState>(
        string filePath,
        TState state,
        Func<Stream, TState, Task> asyncWriter);

    /// <summary>
    /// Makes a child file system that is a sub-directory in the current file system.
    /// </summary>
    /// <param name="childPath">Child path</param>
    /// <param name="defaultCompression">Default compression</param>
    /// <returns><see cref="IFileSystem"/></returns>
    IFileSystem CreateForChildPath(string childPath, FileCompression defaultCompression);

    /// <summary>
    /// Ensures the base path is created.
    /// </summary>
    void EnsurePathCreated();
}