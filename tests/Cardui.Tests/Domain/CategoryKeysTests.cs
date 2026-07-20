using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class CategoryKeysTests
{
    [Theory]
    [InlineData("Dining", "dining")]
    [InlineData("Home Improvement", "home-improvement")]
    [InlineData("  Multiple   Spaces  ", "multiple-spaces")]
    public void CreateFromName_NormalizesDisplayNameToKey(
        string name,
        string expectedKey)
    {
        Assert.Equal(expectedKey, CategoryKeys.CreateFromName(name));
    }
}
