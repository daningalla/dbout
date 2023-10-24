using DbOut.IO;
using Microsoft.Extensions.Logging;
using Vertical.Pipelines;

namespace DbOut.Engine.Pipeline;

public class CleanOutputDirectoryTask : IPipelineMiddleware<JobContext>
{
    private readonly ILogger<CleanOutputDirectoryTask> _logger;
    private readonly IFileSystem _fileSystem;

    public CleanOutputDirectoryTask(ILogger<CleanOutputDirectoryTask> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(
        JobContext context,
        PipelineDelegate<JobContext> next,
        CancellationToken cancellationToken)
    {
        if (!context.Options.Clean)
        {
            await next(context, cancellationToken);
            return;
        }

        {
            using var _ = _logger.BeginScope("(core clean)");
            foreach (var filePath in Directory.EnumerateFiles(_fileSystem.BasePath, "*",
                         SearchOption.AllDirectories))
            {
                _logger.LogDebug("Delete {path}", filePath);
                File.Delete(filePath);
            }
        }

        _logger.LogInformation("Output directory cleaned.");

        await next(context, cancellationToken);
    }
}