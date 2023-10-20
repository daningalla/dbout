using System.Diagnostics.CodeAnalysis;
using DbOut.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DbOut.TokenReplacement;

[ServiceRegistration(ServiceLifetime.Singleton)]
public class SpecialFolderTokenProvider : ITokenProvider
{
    /// <inheritdoc />
    public bool TryResolveValue(string token, [NotNullWhen(true)] out string? value)
    {
        value = null;

        if (!Enum.TryParse(token, out Environment.SpecialFolder specialFolder))
            return false;

        value = Environment.GetFolderPath(specialFolder);
        return !string.IsNullOrWhiteSpace(value);
    }
}