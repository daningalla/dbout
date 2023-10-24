using DbOut.Continuation;
using DbOut.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbOut.Engine.Pipeline;

public class SaveRuntimeOptionsTask : CommandTask
{
    private readonly IRestorePoint _restorePoint;
    private readonly IOptions<RuntimeOptions> _options;

    /// <inheritdoc />
    public SaveRuntimeOptionsTask(
        ILogger<SaveRuntimeOptionsTask> logger,
        IRestorePoint restorePoint,
        IOptions<RuntimeOptions> options) 
        : base(logger, new[] { CommandMode.Execute })
    {
        _restorePoint = restorePoint;
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        var criticalOptions = _restorePoint.CreateCriticalOptions(context.Options);
        await _restorePoint.SaveRuntimeParametersAsync(criticalOptions);
        
        Logger.LogInformation("Runtime options saved to restore point.");
    }
}