using DbOut.Options;
using Microsoft.Extensions.Logging;
using Vertical.Pipelines;

namespace DbOut.Engine.Pipeline;

public abstract class CommandTask : IPipelineMiddleware<JobContext>
{
    private readonly CommandMode[] _commandModes;

    protected CommandTask(ILogger logger, CommandMode[] commandModes) 
    {
        Logger = logger;
        _commandModes = commandModes;
    }

    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task InvokeAsync(
        JobContext context,
        PipelineDelegate<JobContext> next,
        CancellationToken cancellationToken)
    {
        if (!_commandModes.Contains(context.Options.CommandMode))
        {
            Logger.LogDebug("Task {type} skipped.", GetType().Name);
            await next(context, cancellationToken);
            return;
        }

        Logger.LogTrace("Execute command task {type}", GetType());

        await InvokeCoreAsync(context, cancellationToken);
        await next(context, cancellationToken);
    }

    protected virtual Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}