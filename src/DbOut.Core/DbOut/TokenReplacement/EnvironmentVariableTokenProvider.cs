using System.Diagnostics.CodeAnalysis;
using DbOut.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DbOut.TokenReplacement;

[ServiceRegistration(ServiceLifetime.Singleton)]
public class EnvironmentVariableTokenProvider : ITokenProvider
{
    /// <inheritdoc />
    public bool TryResolveValue(string token, [NotNullWhen(true)] out string? value)
    {
        value = Environment.GetEnvironmentVariable(token);
        return !string.IsNullOrWhiteSpace(value);
    }
}