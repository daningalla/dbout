namespace DbOut.TokenReplacement;

/// <summary>
/// Parses and replaces tokens from strings.
/// </summary>
public interface ITokenParser
{
    /// <summary>
    /// Replaces tokens using registered token providers.
    /// </summary>
    /// <param name="source">Source string to parse</param>
    /// <returns>String with replacements</returns>
    string ReplaceTokens(string source);
}