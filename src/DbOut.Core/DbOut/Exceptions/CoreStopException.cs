namespace DbOut.Exceptions;

public class CoreStopException : Exception
{
    public CoreStopException() : base("Execution stopped")
    {
    }
}