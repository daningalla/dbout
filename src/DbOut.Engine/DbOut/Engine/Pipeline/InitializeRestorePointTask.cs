using DbOut.Continuation;
using DbOut.Exceptions;
using DbOut.Options;
using DbOut.Utilities;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class InitializeRestorePointTask : CommandTask
{
    private readonly IRestorePoint _restorePoint;

    public InitializeRestorePointTask(
        ILogger<InitializeQueryEngineTask> logger,
        IRestorePoint restorePoint)
        : base(logger, new[] { CommandMode.Execute })
    {
        _restorePoint = restorePoint;
    }

    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        await _restorePoint.InitializeAsync();
        
        Logger.LogInformation("Initialized restore point.");

        var restorePointParameters = await _restorePoint.GetRuntimeParametersAsync();
        if (restorePointParameters == null)
        {
            Logger.LogDebug("Job parameters not found in restore point");
            return;
        }

        var criticalOptions = _restorePoint.CreateCriticalOptions(context.Options);
        var hash = criticalOptions.Sha();

        if (hash != restorePointParameters.Hash)
        {
            Logger.LogError("Cannot resume from restore point, one or more critical parameters changed.");
            throw new CoreStopException();
        }
        
        Logger.LogInformation("Validated restore point parameters.");
    }
}