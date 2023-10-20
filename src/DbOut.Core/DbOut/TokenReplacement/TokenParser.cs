using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DbOut.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DbOut.TokenReplacement;

[ServiceRegistration(ServiceLifetime.Singleton)]
public partial class TokenParser : ITokenParser
{
    private readonly IEnumerable<ITokenProvider> _tokenProviders;
    private readonly ILogger<TokenParser> _logger;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    [GeneratedRegex(@"\$\((\w+)\)")]
    private static partial Regex PlaceholderRegex();

    public TokenParser(IEnumerable<ITokenProvider> tokenProviders, ILogger<TokenParser> logger)
    {
        _tokenProviders = tokenProviders;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ReplaceTokens(string source)
    {
        return _cache.GetOrAdd(source, ReplaceTokensInSource);
    }

    private string ReplaceTokensInSource(string source)
    {
        return PlaceholderRegex().Replace(source, match =>
        {
            var token = match.Groups[1].Value;
            
            foreach (var provider in _tokenProviders)
            {
                if (!provider.TryResolveValue(token, out var value))
                    continue;

                _logger.LogTrace("Replaced token {token} in source {source}, value='{value}'",
                    token,
                    source,
                    value);
                
                return value;
            }

            return match.Value;
        });
    }
}