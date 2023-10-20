using System.Diagnostics.CodeAnalysis;

namespace DbOut.TokenReplacement;

public interface ITokenProvider
{
    /// <summary>
    /// Tries to resolve a value.
    /// </summary>
    /// <param name="token">Token value.</param>
    /// <param name="value">If handled, the replacement value.</param>
    /// <returns>Whether the operation succeeded.</returns>
    bool TryResolveValue(string token, [NotNullWhen(true)] out string? value);
}