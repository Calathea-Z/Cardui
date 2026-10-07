namespace Cardui.Api.Domain.Debts;

/// <summary>
/// The credit limit a debt uses, and where it came from.
/// Limit is the amount in use. SyncedLimit is the account figure beside it, when the connection provides one.
/// </summary>
public sealed record DebtCreditLimitResolution(
    decimal? Limit,
    DebtFieldSource Source,
    decimal? SyncedLimit,
    DateOnly? SyncedAsOf);
