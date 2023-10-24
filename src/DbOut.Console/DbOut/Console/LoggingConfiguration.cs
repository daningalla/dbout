using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Vertical.SpectreLogger;
using Vertical.SpectreLogger.Options;
using Vertical.SpectreLogger.Rendering;

namespace DbOut.Console;

public static class LoggingConfiguration
{
    public static IServiceCollection ConfigureLogging(this IServiceCollection services, ProgramArguments arguments)
    {
        services.AddLogging(logger =>
        {
            logger
                .AddSpectreConsole(config => config
                    .SetMinimumLevel(arguments.LogLevel)
                    .ConfigureProfile(LogLevel.Trace,
                        profile => profile.OutputTemplate =
                            "[grey35][[{DateTime:T} Trce]] {Scopes}{Message}{NewLine}{Exception}[/]")
                    .ConfigureProfile(LogLevel.Debug,
                        profile => profile.OutputTemplate =
                            "[grey46][[{DateTime:T} Dbug]] {Scopes}{Message}{NewLine}{Exception}[/]")
                    .ConfigureProfile(LogLevel.Information,
                        profile => profile.OutputTemplate =
                            "[grey85][[{DateTime:T} [green3_1]Info[/]]] {Scopes}{Message}{NewLine}{Exception}[/]")
                    .ConfigureProfile(LogLevel.Warning,
                        profile => profile.OutputTemplate =
                            "[grey85][[{DateTime:T} [gold1]Warn[/]]] {Scopes}{Message}{NewLine}{Exception}[/]")
                    .ConfigureProfile(LogLevel.Error,
                        profile => profile.OutputTemplate =
                            "[grey85][[{DateTime:T} [red1]Fail[/]]] {Scopes}{Message}{NewLine}{Exception}[/]")
                    .ConfigureProfile(LogLevel.Critical,
                        profile => profile.OutputTemplate = 
                            "[[[red1]{DateTime:T}[/] [white on red1]Crit[/]]] [red3] {Scopes}{Message}{NewLine}{Exception}[/]")
                    .ConfigureProfiles(profile => profile.ConfigureOptions<ExceptionRenderer.Options>(ex => ex.MaxStackFrames = 25))
                )
                .SetMinimumLevel(arguments.LogLevel);

            if (string.IsNullOrWhiteSpace(arguments.FileLogPath)) 
                return;

            if (!Directory.Exists(arguments.FileLogPath))
                Directory.CreateDirectory(arguments.FileLogPath);
            
            var serilogLevel = arguments.LogLevel switch
            {
                LogLevel.Trace => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                _ => LogEventLevel.Fatal
            };
            
            logger.AddSerilog(new LoggerConfiguration()
                .MinimumLevel.Is(serilogLevel)
                .WriteTo.File(
                    arguments.FileLogPath,
                    restrictedToMinimumLevel: serilogLevel,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: arguments.MaxLogFileSize.ComputedLength)
                .CreateLogger());
        });

        return services;
    }
}