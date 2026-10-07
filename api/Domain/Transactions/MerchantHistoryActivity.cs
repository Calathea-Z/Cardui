namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// One merchant transaction reduced to the date and amount a history period needs.
/// </summary>
public readonly record struct MerchantHistoryActivity(DateOnly Date, decimal Amount);
