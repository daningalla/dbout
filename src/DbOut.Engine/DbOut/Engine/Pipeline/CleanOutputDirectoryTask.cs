using DbOut.Continuation;
using DbOut.Exceptions;
using DbOut.IO;
using Microsoft.Extensions.Logging;
using Vertical.Pipelines;

namespace DbOut.Engine.Pipeline;

public class CleanOutputDirectoryTask : IPipelineMiddleware<JobContext>
{
    private readonly ILogger<CleanOutputDirectoryTask> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IInteractiveConfirmation? _interactiveConfirmation;

    public CleanOutputDirectoryTask(ILogger<CleanOutputDirectoryTask> logger, 
        IFileSystem fileSystem,
        IInteractiveConfirmation? interactiveConfirmation = null)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _interactiveConfirmation = interactiveConfirmation;
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

        if (!Directory.Exists(_fileSystem.BasePath))
        {
            _logger.LogDebug("Base directory not found, clean evaluation skipped.");
            await next(context, cancellationToken);
            return;
        }

        var filePaths = Directory.GetFiles(_fileSystem.BasePath, "*", SearchOption.AllDirectories);
        var prompt = new[]
        {
            "All files in the output directory will be deleted including cache data files,",
            "completed output files, and the restore point database. If confirmed, all files",
            "will be removed and the job will begin from the top.",
            "",
            $"Delete {filePaths.Length} file(s) in {_fileSystem.BasePath}?"
        };
        var interactiveConfirm = _interactiveConfirmation?.Confirm(string.Join(Environment.NewLine, prompt));

        if (interactiveConfirm.HasValue && !interactiveConfirm.Value)
        {
            _logger.LogInformation("Operation aborted by interactive input.");
            throw new CoreStopException();
        }
        
        {
            using var _ = _logger.BeginScope("(core clean)");
            foreach (var filePath in filePaths)
            {
                _logger.LogDebug("Delete {path}", filePath);
                File.Delete(filePath);
            }
        }

        _logger.LogInformation("Output directory cleaned.");

        await next(context, cancellationToken);
    }
}