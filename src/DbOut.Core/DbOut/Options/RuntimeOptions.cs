using DbOut.Exceptions;

namespace DbOut.Options;

public class RuntimeOptions
{
    public required CommandMode CommandMode { get; init; }
    
    public ConnectionOptions? ConnectionOptions { get; init; }
    
    public OutputOptions? Output { get; init; }
    
    public DataSourceOptions? DataSource { get; init; }
    
    public ParallelizationOptions? Parallelization { get; init; }
    
    public string? OutputPath { get; init; }
    
    public bool Clean { get; init; }

    public static string ThrowIfNullOrEmpty(string option, string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            throw ExceptionFormatter.RequiredOptionNotConfigured(option);
        }

        return arg;
    }

    public static T ThrowIfNullReference<T>(string option, T? arg) where T : class
    {
        if (ReferenceEquals(null, arg))
        {
            throw ExceptionFormatter.RequiredOptionNotConfigured(option);
        }

        return arg;
    }

    public static T ThrowIfNullStruct<T>(string option, T? arg) where T : struct
    {
        if (!arg.HasValue)
        {
            throw ExceptionFormatter.RequiredOptionNotConfigured(option);
        }

        return arg.Value;
    }
}