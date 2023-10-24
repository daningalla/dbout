using DbOut.Continuation;
using DbOut.Options;
using DbOut.Reporting;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class SaveTelemetryTask : CommandTask
{
    private readonly ITelemetryListener _telemetryListener;
    private readonly IRestorePoint _restorePoint;

    /// <inheritdoc />
    public SaveTelemetryTask(ILogger<SaveTelemetryTask> logger, 
        ITelemetryListener telemetryListener,
        IRestorePoint restorePoint) 
        : base(logger, new[] { CommandMode.Execute })
    {
        _telemetryListener = telemetryListener;
        _restorePoint = restorePoint;
    }

    /// <inheritdoc />
    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        var telemetrySnapshot = _telemetryListener.GetSnapshot();
        await _restorePoint.SaveTelemetryAsync(telemetrySnapshot);
        
        Logger.LogInformation("Telemetry saved to restore point.");
    }
}