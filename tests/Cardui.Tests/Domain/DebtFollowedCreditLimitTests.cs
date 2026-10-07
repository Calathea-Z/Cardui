using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtFollowedCreditLimitTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    [Fact]
    public void Resolve_KeepsTheRecordedLimitWhenTheDebtIsNotFollowing()
    {
        var resolution = DebtFollowedCreditLimit.Resolve(Facts(
            following: false,
            recorded: 1000m,
            synced: 5000m));

        Assert.Equal(DebtFieldSource.Manual, resolution.Source);
        Assert.Equal(1000m, resolution.Limit);
        Assert.Null(resolution.SyncedLimit);
    }

    [Fact]
    public void Resolve_UsesAUsableCardLimitAndRoundsIt()
    {
        var resolution = DebtFollowedCreditLimit.Resolve(Facts(recorded: 1000m, synced: 5000.004m));

        Assert.Equal(DebtFieldSource.Synced, resolution.Source);
        Assert.Equal(5000.00m, resolution.Limit);
        Assert.Equal(5000.00m, resolution.SyncedLimit);
        Assert.Equal(Today, resolution.SyncedAsOf);
    }

    [Fact]
    public void Resolve_KeepsAnOverrideEvenWhenItMatchesTheSyncedLimit()
    {
        var resolution = DebtFollowedCreditLimit.Resolve(Facts(
            recorded: 5000m,
            synced: 5000m,
            overridden: true));

        Assert.Equal(DebtFieldSource.Override, resolution.Source);
        Assert.Equal(5000m, resolution.Limit);
        Assert.Equal(5000m, resolution.SyncedLimit);
    }

    [Fact]
    public void Resolve_UsesThePersonsLimitWhenTheConnectionGivesNone()
    {
        foreach (var synced in new decimal?[] { null, 0m, -20m, 100_000_000.01m })
        {
            var resolution = DebtFollowedCreditLimit.Resolve(Facts(recorded: 1000m, synced: synced));
            Assert.Equal(DebtFieldSource.Manual, resolution.Source);
            Assert.Equal(1000m, resolution.Limit);
            Assert.Null(resolution.SyncedLimit);
        }
    }

    [Fact]
    public void Resolve_DoesNotFollowALimitOntoAnInstallmentDebt()
    {
        var resolution = DebtFollowedCreditLimit.Resolve(Facts(
            recorded: null,
            synced: 8000m,
            kind: DebtKind.Installment));

        Assert.Equal(DebtFieldSource.Manual, resolution.Source);
        Assert.Null(resolution.Limit);
        Assert.Null(resolution.SyncedLimit);
    }

    [Fact]
    public void RecordedDiffers_IgnoresAMissingAmount()
    {
        Assert.False(DebtFollowedCreditLimit.RecordedDiffers(null, 5000m));
        Assert.False(DebtFollowedCreditLimit.RecordedDiffers(1000m, null));
        Assert.False(DebtFollowedCreditLimit.RecordedDiffers(5000.001m, 5000m));
        Assert.True(DebtFollowedCreditLimit.RecordedDiffers(1000m, 5000m));
    }

    private static DebtCreditLimitFacts Facts(
        decimal? recorded,
        decimal? synced,
        bool following = true,
        bool overridden = false,
        DebtKind kind = DebtKind.Revolving)
    {
        return new DebtCreditLimitFacts(
            following,
            overridden,
            kind,
            recorded,
            synced,
            Today);
    }
}
