using Vertical.CommandLine;

namespace DbOut.Console;

internal static class ExceptionFormatter
{
    public static Exception InvalidAdHocConnectionProperty(string value)
    {
        return new UsageException($"Ad-hoc connection property (--provider-prop) invalid: '{value}'");
    }

    public static Exception MissingRequiredArgument(string argTemplate)
    {
        return new UsageException($"Missing required command line argument {argTemplate}");
    }

    public static Exception ConflictingConnectionParameter(string argTemplate)
    {
        return new UsageException(
            $"Argument {argTemplate} cannot be used when connection string (-c|--connection-string) " +
            "is specified.");
    }

    public static Exception InvalidRetryInterval(string argTemplate, string value)
    {
        return new UsageException(
            $"Invalid retry interval value for {argTemplate}: '{value}'");
    }

    public static Exception InvalidFileSize(string argTemplate, string? argumentsMaxFileSize)
    {
        return new UsageException($"Invalid value for {argTemplate} '{argumentsMaxFileSize}'");
    }
}