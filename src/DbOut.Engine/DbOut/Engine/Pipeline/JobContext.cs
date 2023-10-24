using DbOut.Options;
using DbOut.Services;

namespace DbOut.Engine.Pipeline;

public class JobContext
{
    public required RuntimeOptions Options { get; init; }
    public required IRuntimeServices Services { get; init; }
}