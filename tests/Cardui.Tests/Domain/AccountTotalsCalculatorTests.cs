using Cardui.Api.Domain.Accounts;
using Xunit;

namespace Cardui.Tests.Domain;

public class AccountTotalsCalculatorTests
{
    [Fact]
    public void Calculate_ClassifiesAssetsAndLiabilitiesIgnoringCase()
    {
        var totals = AccountTotalsCalculator.Calculate(
        [
            new AccountBalanceValue("DEPOSITORY", 1_200m),
            new AccountBalanceValue("investment", 4_000m),
            new AccountBalanceValue("credit", 700m),
            new AccountBalanceValue("LOAN", 2_500m),
            new AccountBalanceValue("other", 10_000m)
        ]);

        Assert.Equal(1_200m, totals.Cash);
        Assert.Equal(4_000m, totals.Investments);
        Assert.Equal(700m, totals.CreditCards);
        Assert.Equal(2_500m, totals.Loans);
        Assert.Equal(5_200m, totals.Assets);
        Assert.Equal(3_200m, totals.Liabilities);
        Assert.Equal(2_000m, totals.NetWorth);
    }
}
