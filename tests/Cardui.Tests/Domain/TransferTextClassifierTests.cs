using Cardui.Api.Domain;
using Xunit;

namespace Cardui.Tests.Domain;

public class TransferTextClassifierTests
{
    [Theory]
    [InlineData("Online Transfer From Checking")]
    [InlineData("Internal transfer")]
    [InlineData("XFER TO SAVINGS")]
    public void LooksLikeBankTransfer_MatchesDirectBankTransferPhrases(string text)
    {
        Assert.True(TransferTextClassifier.LooksLikeBankTransfer(null, text));
    }

    [Theory]
    [InlineData("Venmo")]
    [InlineData("Zelle payment")]
    [InlineData("PayPal transfer")]
    public void LooksLikeTransferPairSignal_IncludesPaymentRailHints(string text)
    {
        Assert.True(TransferTextClassifier.LooksLikeTransferPairSignal(null, text));
    }
}
