using Cardui.Api.Domain.Accounts;
using Xunit;

namespace Cardui.Tests.Domain;

public class AccountTypesTests
{
    [Theory]
    [InlineData("depository")]
    [InlineData("DEPOSITORY")]
    public void IsCash_MatchesDepositoryIgnoringCase(string type)
    {
        Assert.True(AccountTypes.IsCash(type));
    }

    [Theory]
    [InlineData("depository")]
    [InlineData("investment")]
    public void IsOwnedAsset_AllowsDepositoryAndInvestmentAccounts(string type)
    {
        Assert.True(AccountTypes.IsOwnedAsset(type));
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("loan")]
    public void IsOwnedAsset_ExcludesLiabilityAccounts(string type)
    {
        Assert.False(AccountTypes.IsOwnedAsset(type));
    }
}
