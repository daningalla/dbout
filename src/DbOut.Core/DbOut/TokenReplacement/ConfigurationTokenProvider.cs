using System.Diagnostics.CodeAnalysis;
using DbOut.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DbOut.TokenReplacement;

[ServiceRegistration(ServiceLifetime.Singleton)]
public class ConfigurationTokenProvider : ITokenProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationTokenProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /// <inheritdoc />
    public bool TryResolveValue(string token, [NotNullWhen(true)] out string? value)
    {
        value = _configuration[token];
        return !string.IsNullOrWhiteSpace(value);
    }
}