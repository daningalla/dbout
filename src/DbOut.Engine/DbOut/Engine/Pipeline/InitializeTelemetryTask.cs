using DbOut.Continuation;
using DbOut.Options;
using DbOut.Reporting;
using Microsoft.Extensions.Logging;

namespace DbOut.Engine.Pipeline;

public class InitializeTelemetryTask : CommandTask
{
    private readonly IRestorePoint _restorePoint;
    private readonly ITelemetryListener _telemetryListener;

    /// <inheritdoc />
    public InitializeTelemetryTask(ILogger<InitializeTelemetryTask> logger,
        IRestorePoint restorePoint,
        ITelemetryListener telemetryListener) 
        : base(logger, new[] { CommandMode.Execute })
    {
        _restorePoint = restorePoint;
        _telemetryListener = telemetryListener;
    }

    /// <inheritdoc />
    protected override async Task InvokeCoreAsync(JobContext context, CancellationToken cancellationToken)
    {
        var telemetryData = await _restorePoint.LoadTelemetryAsync();
        _telemetryListener.InitializeWith(telemetryData);
        
        Logger.LogInformation("Telemetry listener initialized.");
    }
}