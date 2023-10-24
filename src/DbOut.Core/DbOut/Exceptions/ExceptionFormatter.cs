namespace DbOut.Exceptions;

internal static class ExceptionFormatter
{
    public static Exception RequiredOptionNotConfigured(string option)
    {
        return new ArgumentException($"Required option {option} has not been configured.");
    }
}