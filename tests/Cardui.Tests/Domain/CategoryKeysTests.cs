using Cardui.Api.Domain.Categories;
using Xunit;

namespace Cardui.Tests.Domain;

public class CategoryKeysTests
{
    [Theory]
    [InlineData("Dining", "dining")]
    [InlineData("Home Improvement", "home-improvement")]
    [InlineData("  Multiple   Spaces  ", "multiple-spaces")]
    [InlineData("Food & Dining", "food-dining")]
    [InlineData("Bills & Utilities", "bills-utilities")]
    public void CreateFromName_NormalizesDisplayNameToKey(
        string name,
        string expectedKey)
    {
        Assert.Equal(expectedKey, CategoryKeys.CreateFromName(name));
    }
}
