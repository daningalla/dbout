using DbOut.IO;
using DbOut.Options;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class PrepareFileSystemTask : CommandTask
{
    private readonly IFileSystem _fileSystem;

    public PrepareFileSystemTask(ILoggerFactory loggerFactory, IFileSystem fileSystem) 
        : base (loggerFactory.CreateLogger<PrepareFileSystemTask>(), new[] { CommandMode.Execute })
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    protected override Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        _fileSystem.EnsurePathCreated();

        if (_fileSystem.BasePath == Directory.GetCurrentDirectory())
        {
            Logger.LogWarning("Output path not specified - current working directory will be used.");
        }
        
        Logger.LogInformation("File system initialized.");

        return Task.CompletedTask;
    }
}