using DbOut.Engine.Pipeline;
using DbOut.Exceptions;
using DbOut.Options;
using DbOut.Reporting;
using DbOut.Services;
using DbOut.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vertical.Pipelines;

namespace DbOut.Engine;

[Service]
public class DbOutEngine
{
    private readonly ILogger<DbOutEngine> _logger;
    private readonly IPipelineFactory<JobContext> _pipelineFactory;
    private readonly IOptions<RuntimeOptions> _options;
    private readonly IRuntimeServices _runtimeServices;

    public DbOutEngine(
        ILogger<DbOutEngine> logger,
        IPipelineFactory<JobContext> pipelineFactory,
        IOptions<RuntimeOptions> options,
        IRuntimeServices runtimeServices)
    {
        _logger = logger;
        _pipelineFactory = pipelineFactory;
        _options = options;
        _runtimeServices = runtimeServices;
    }

    public Version Version => new Version(1, 0, 0);
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var pipeline = _pipelineFactory.CreatePipeline();
        var context = new JobContext
        {
            Options = _options.Value,
            Services = _runtimeServices
        };
        
        _logger.LogTrace("Runtime options provided by {type}", _options.GetType());
        _logger.LogTrace("Command mode = {mode}", context.Options.CommandMode);

        try
        {
            await pipeline(context, cancellationToken);
        }
        catch (CoreStopException exception)
        {
            _logger.LogError(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Un unhandled exception occurred.");
        }
    }
}