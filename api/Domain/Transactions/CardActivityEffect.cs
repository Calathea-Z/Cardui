namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// The spending, cash, and debt effect of one transaction.
/// SpendingAmount is the signed category amount before a month is floored at zero.
/// CashChange is negative when cash leaves a depository account.
/// DebtChange is positive when the amount owed on a credit card rises.
/// A card payment keeps SpendingAmount at zero so the purchase is not counted again.
/// </summary>
public sealed record CardActivityEffect(
    CardActivityKind Kind,
    decimal SpendingAmount,
    decimal CashChange,
    decimal DebtChange);
