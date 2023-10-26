// See https://aka.ms/new-console-template for more information

using DbOut.Console;
using DbOut.Continuation;
using DbOut.Engine;
using DbOut.Engine.Services;
using DbOut.Options;
using DbOut.Providers.MySql.Services;
using DbOut.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vertical.CommandLine;

var arguments = CommandLineApplication.ParseArguments<ProgramArguments>(
    new ProgramArgumentsConfiguration(),
    args);

//var arguments = DebugArguments.Debug();

await using var services = new ServiceCollection()
    .ConfigureLogging(arguments)
    .AddSingleton(arguments)
    .AddSingleton<IOptions<RuntimeOptions>, RuntimeOptionsAdapter>()
    .AddSingleton<IInteractiveConfirmation>(new ConsoleInteractiveConfirmation(arguments.CleanMode))
    .AddDbOutCore()
    .AddDbOutEngine(builder => builder.AddMySql())
    .BuildServiceProvider();

var engine = services.GetRequiredService<DbOutEngine>();

Console.WriteLine();
Console.WriteLine("-----------------------------------");
Console.WriteLine("DbOut engine (C) Metalware Software");
Console.WriteLine($"Version {engine.Version}");
Console.WriteLine("-----------------------------------");

await engine.RunAsync(CancellationToken.None);

Console.WriteLine();