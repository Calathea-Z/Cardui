using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Domain;

/// <summary>
/// The balance a debt uses, and where it came from.
/// Balance is the amount in use. SyncedBalance is the account figure beside it, when there is one.
/// Credit is the positive amount of a card credit counted as zero. It is null when the balance is not a credit.
/// Freshness is null when the debt is not following an account.
/// </summary>
public sealed record DebtBalanceResolution(
    decimal? Balance,
    DateOnly? AsOf,
    DebtFieldSource Source,
    decimal? SyncedBalance,
    DateOnly? SyncedAsOf,
    DebtAccountBalanceBlock Block,
    decimal? Credit,
    DebtLinkFreshness? Freshness,
    DateOnly? SyncFailedOn);
