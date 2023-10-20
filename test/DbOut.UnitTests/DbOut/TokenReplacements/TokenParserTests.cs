using DbOut.TokenReplacement;
using FluentAssertions;

namespace DbOut.TokenReplacements;

public class TokenParserTests : ServiceHarness<ITokenParser>
{
    [Fact]
    public void ExpectedTokensReplaced()
    {
        var guid = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable("DBOUT_VALUE", guid, EnvironmentVariableTarget.Process);
        
        var result = Instance.ReplaceTokens("$(UserProfile)/$(DBOUT_VALUE)");
        var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        result.Should().Be($"{path}/{guid}");
    }
}