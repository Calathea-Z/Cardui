using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtFollowedBalanceTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    [Fact]
    public void Resolve_KeepsTheRecordedBalanceWhenTheDebtIsNotFollowing()
    {
        var resolution = DebtFollowedBalance.Resolve(Facts(
            following: false,
            recorded: 80m,
            synced: 120m,
            syncedOn: Today));

        Assert.Equal(DebtFieldSource.Manual, resolution.Source);
        Assert.Equal(80m, resolution.Balance);
        Assert.Null(resolution.Freshness);
        Assert.Null(resolution.SyncedBalance);
    }

    [Fact]
    public void Resolve_UsesARecentSnapshotAndCountsACardCreditAsZero()
    {
        var owed = DebtFollowedBalance.Resolve(Facts(recorded: 80m, synced: 120.004m, syncedOn: Today));
        Assert.Equal(DebtFieldSource.Synced, owed.Source);
        Assert.Equal(120.00m, owed.Balance);
        Assert.Equal(Today, owed.AsOf);
        Assert.Equal(DebtLinkFreshness.Current, owed.Freshness);
        Assert.Null(owed.Credit);

        var credit = DebtFollowedBalance.Resolve(Facts(recorded: 80m, synced: -42.10m, syncedOn: Today));
        Assert.Equal(0m, credit.Balance);
        Assert.Equal(-42.10m, credit.SyncedBalance);
        Assert.Equal(42.10m, credit.Credit);
        Assert.Equal(DebtAccountBalanceBlock.None, credit.Block);
        Assert.Equal(DebtLinkFreshness.Current, credit.Freshness);

        var loan = DebtFollowedBalance.Resolve(Facts(
            kind: DebtKind.Installment,
            recorded: 80m,
            synced: -42.10m,
            syncedOn: Today));
        Assert.Equal(80m, loan.Balance);
        Assert.Equal(DebtFieldSource.Manual, loan.Source);
        Assert.Equal(DebtAccountBalanceBlock.NegativeBalance, loan.Block);
        Assert.Equal(DebtLinkFreshness.Stale, loan.Freshness);
    }

    [Fact]
    public void Resolve_KeepsAnOverrideAndMarksABlockedOrOldBalanceStale()
    {
        var kept = DebtFollowedBalance.Resolve(Facts(
            recorded: 80m,
            recordedOn: new DateOnly(2026, 10, 1),
            synced: 120m,
            syncedOn: Today,
            overridden: true));
        Assert.Equal(DebtFieldSource.Override, kept.Source);
        Assert.Equal(80m, kept.Balance);
        Assert.Equal(120m, kept.SyncedBalance);
        Assert.Equal(DebtLinkFreshness.Current, kept.Freshness);

        var old = DebtFollowedBalance.Resolve(Facts(
            recorded: 80m,
            synced: 120m,
            syncedOn: Today.AddDays(-3)));
        Assert.Equal(DebtLinkFreshness.Stale, old.Freshness);
        Assert.Equal(120m, old.Balance);

        var recent = DebtFollowedBalance.Resolve(Facts(
            recorded: 80m,
            synced: 120m,
            syncedOn: Today.AddDays(-2)));
        Assert.Equal(DebtLinkFreshness.Current, recent.Freshness);

        var otherCurrency = DebtFollowedBalance.Resolve(Facts(
            recorded: 80m,
            synced: 120m,
            syncedOn: Today,
            syncedCurrency: "CAD"));
        Assert.Equal(80m, otherCurrency.Balance);
        Assert.Equal(DebtAccountBalanceBlock.CurrencyDiffers, otherCurrency.Block);
        Assert.Equal(DebtLinkFreshness.Stale, otherCurrency.Freshness);
    }

    [Fact]
    public void Resolve_PrefersAMissingAccountThenARemovedLinkThenAFailedSync()
    {
        var missing = DebtFollowedBalance.Resolve(Facts(
            recorded: 80m,
            synced: 120m,
            syncedOn: Today.AddDays(-4),
            active: false,
            bankLinked: false));
        Assert.Equal(DebtLinkFreshness.AccountMissing, missing.Freshness);

        var removed = DebtFollowedBalance.Resolve(Facts(
            recorded: 80m,
            synced: 120m,
            syncedOn: new DateOnly(2026, 10, 4),
            bankLinked: false));
        Assert.Equal(DebtLinkFreshness.Disconnected, removed.Freshness);
        Assert.Equal(120m, removed.Balance);

        var failedOn = new DateOnly(2026, 10, 5);
        var failed = DebtFollowedBalance.Resolve(Facts(
            recorded: 80m,
            synced: 120m,
            syncedOn: new DateOnly(2026, 10, 4),
            completedAt: new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero),
            failedAt: new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero),
            syncFailedOn: failedOn));
        Assert.Equal(DebtLinkFreshness.SyncFailing, failed.Freshness);
        Assert.Equal(failedOn, failed.SyncFailedOn);
        Assert.Equal(120m, failed.Balance);
    }

    [Fact]
    public void RecordedDiffers_IgnoresAMissingAmountAndAMatchingCent()
    {
        Assert.False(DebtFollowedBalance.RecordedDiffers(null, 10m));
        Assert.False(DebtFollowedBalance.RecordedDiffers(10m, null));
        Assert.False(DebtFollowedBalance.RecordedDiffers(10.004m, 10.00m));
        Assert.True(DebtFollowedBalance.RecordedDiffers(10m, 0m));
    }

    [Fact]
    public void CanFollow_RequiresAnActiveConnectedCardOrLoanInTheSameCurrency()
    {
        Assert.True(Eligible());
        Assert.False(Eligible(source: FinancialRecordSource.Manual));
        Assert.False(Eligible(hasPlaidItem: false));
        Assert.False(Eligible(isActive: false));
        Assert.False(Eligible(isArchived: true));
        Assert.False(Eligible(type: AccountTypes.Depository));
        Assert.False(Eligible(accountCurrency: "CAD"));
        Assert.False(Eligible(accountCurrency: null));
        Assert.True(Eligible(type: AccountTypes.Loan));
    }

    [Fact]
    public void Calculate_CountsAFollowedBalanceAndSkipsTheSideBySideChoice()
    {
        var followed = new DebtSummaryInput(
            Guid.NewGuid(),
            "USD",
            DebtKind.Revolving,
            120m,
            Today,
            12m,
            null,
            null,
            null,
            null,
            null,
            null,
            new DebtLinkedBalance(80m, Today, "USD", true),
            true,
            DebtLinkFreshness.Stale);
        var reference = new DebtSummaryInput(
            Guid.NewGuid(),
            "USD",
            DebtKind.Revolving,
            50m,
            Today,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            new DebtLinkedBalance(40m, Today, "USD", true));

        var report = DebtSummary.Calculate([followed, reference], Today);

        Assert.Null(report.Debts.Single(item => item.DebtId == followed.DebtId).BalanceComparison);
        Assert.NotNull(report.Debts.Single(item => item.DebtId == reference.DebtId).BalanceComparison);
        var totals = Assert.Single(report.Currencies);
        Assert.Equal(170m, totals.RecordedBalance);
        Assert.Equal(1, totals.StaleCount);
    }

    private static bool Eligible(
        FinancialRecordSource source = FinancialRecordSource.Plaid,
        bool hasPlaidItem = true,
        bool isActive = true,
        bool isArchived = false,
        string type = AccountTypes.Credit,
        string? accountCurrency = "USD")
    {
        return DebtFollowEligibility.CanFollow(
            source,
            hasPlaidItem,
            isActive,
            isArchived,
            type,
            accountCurrency,
            "USD");
    }

    private static DebtBalanceFacts Facts(
        bool following = true,
        bool overridden = false,
        DebtKind kind = DebtKind.Revolving,
        decimal? recorded = 100m,
        DateOnly? recordedOn = null,
        decimal? synced = null,
        DateOnly? syncedOn = null,
        string? syncedCurrency = "USD",
        bool active = true,
        bool archived = false,
        bool bankLinked = true,
        DateTimeOffset? completedAt = null,
        DateTimeOffset? failedAt = null,
        DateOnly? syncFailedOn = null)
    {
        DebtLinkedBalance? linked = synced is decimal amount
            ? new DebtLinkedBalance(amount, syncedOn, syncedCurrency, true)
            : null;
        return new DebtBalanceFacts(
            following,
            overridden,
            kind,
            "USD",
            recorded,
            recorded is null ? null : recordedOn ?? new DateOnly(2026, 10, 1),
            linked,
            active,
            archived,
            bankLinked,
            completedAt ?? (failedAt is null ? Today.ToDateTime(TimeOnly.MinValue) : null),
            failedAt,
            syncFailedOn,
            Today);
    }
}
