using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtAccountMatcherTests
{
    [Fact]
    public void Suggest_RanksDigitsThenSharedWordsThenACloserBalance()
    {
        var digits = Candidate("Visa", mask: "4821", datedBalance: 9000m);
        var words = Candidate(
            "Sapphire Preferred",
            officialName: "Chase Sapphire",
            datedBalance: 9000m);
        var close = Candidate("Everyday", datedBalance: 1278.18m);
        var farther = Candidate("Spare", datedBalance: 1289.18m);
        var loan = Candidate(
            "Chase Sapphire",
            mask: "4821",
            accountType: AccountTypes.Loan,
            datedBalance: 1240.18m);

        var suggestions = DebtAccountMatcher.Suggest(
            DebtKind.Revolving,
            "Chase Sapphire 4821",
            1240.18m,
            [farther, close, loan, words, digits]);

        Assert.Equal(
            [digits.AccountId, words.AccountId, close.AccountId],
            suggestions.Select(item => item.AccountId));
        Assert.Equal([1, 2, 3], suggestions.Select(item => item.Order));
        Assert.DoesNotContain(suggestions, item => item.AccountId == farther.AccountId);
        Assert.DoesNotContain(suggestions, item => item.AccountId == loan.AccountId);

        Assert.Equal("4821", Assert.Single(suggestions[0].Reasons).Mask);
        Assert.Equal(["Sapphire", "Chase"], Assert.Single(suggestions[1].Reasons).Words);
        Assert.Equal(38m, Assert.Single(suggestions[2].Reasons).Difference);
    }

    [Fact]
    public void Suggest_UsesTheInstitutionAndSkipsAGenericWordOrADigitInsideALongerNumber()
    {
        var institution = Candidate(
            "Remaining",
            accountType: AccountTypes.Loan,
            institutionName: "PenFed");
        var generic = Candidate("Credit card", accountType: AccountTypes.Loan);
        var buried = Candidate("Other", mask: "4821", accountType: AccountTypes.Loan);
        var card = Candidate("Penfed", accountType: AccountTypes.Credit);

        var suggestions = DebtAccountMatcher.Suggest(
            DebtKind.Installment,
            "Auto loan from Penfed 14821",
            null,
            [institution, generic, buried, card]);

        var suggestion = Assert.Single(suggestions);
        Assert.Equal(institution.AccountId, suggestion.AccountId);
        Assert.Equal(["Penfed"], Assert.Single(suggestion.Reasons).Words);
    }

    [Fact]
    public void Suggest_DoesNotMatchAWordInsideALongerWord()
    {
        var suggestions = DebtAccountMatcher.Suggest(
            DebtKind.Revolving,
            "Visalia",
            null,
            [Candidate("Visa")]);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void Suggest_TreatsAnExactBalanceAsCloseAndIgnoresAnUndatedOrDistantOne()
    {
        var same = Candidate("Everyday", datedBalance: 100m);
        var undated = Candidate("Missing");
        var distant = Candidate("Other", datedBalance: 1000m);

        var suggestions = DebtAccountMatcher.Suggest(
            DebtKind.Revolving,
            "Store card",
            100m,
            [same, undated, distant]);

        var suggestion = Assert.Single(suggestions);
        Assert.Equal(same.AccountId, suggestion.AccountId);
        Assert.Equal(0m, Assert.Single(suggestion.Reasons).Difference);
    }

    [Fact]
    public void Suggest_AllowsAWiderGapOnALargeBalanceAndKeepsTheDebtSpelling()
    {
        var wide = Candidate("Car loan", accountType: AccountTypes.Loan, datedBalance: 10400m);
        var tooWide = Candidate("Other", accountType: AccountTypes.Loan, datedBalance: 12000m);

        var suggestions = DebtAccountMatcher.Suggest(
            DebtKind.Installment,
            "car loan",
            10000m,
            [wide, tooWide]);

        var suggestion = Assert.Single(suggestions);
        Assert.Equal(wide.AccountId, suggestion.AccountId);
        Assert.Equal(["car"], suggestion.Reasons[0].Words);
        Assert.Equal(400m, suggestion.Reasons[1].Difference);
    }

    private static DebtMatchCandidate Candidate(
        string name,
        string? officialName = null,
        string? institutionName = null,
        string? mask = null,
        string accountType = AccountTypes.Credit,
        decimal? datedBalance = null)
    {
        return new DebtMatchCandidate(
            Guid.NewGuid(),
            name,
            officialName,
            institutionName,
            mask,
            accountType,
            datedBalance);
    }
}
