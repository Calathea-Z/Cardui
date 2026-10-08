namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One debt's balance right after one modeled payment.
/// DueDate is the date of that payment. Balance is what remains, and zero once the debt is paid off.
/// Interest is the interest charged that month, and Payment is what was paid, including extra and rolled cash.
/// </summary>
public sealed record PayoffBalancePoint(
    Guid DebtId,
    DateOnly DueDate,
    decimal Balance,
    decimal Interest,
    decimal Payment);
