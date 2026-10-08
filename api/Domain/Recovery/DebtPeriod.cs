namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One monthly payment on a debt.
/// Interest is one month of simple interest on StartingBalance. Payment is the cash that leaves.
/// Principal is Payment minus Interest, and it is negative when the payment does not cover interest.
/// MinimumPaid is capped at the amount owed, so a stored minimum larger than the payoff is not taken in cash.
/// </summary>
public sealed record DebtPeriod(
    DateOnly DueDate,
    decimal StartingBalance,
    decimal Interest,
    decimal MinimumPaid,
    decimal ExtraPaid,
    decimal Payment,
    decimal Principal,
    decimal EndingBalance,
    decimal RatePercent,
    bool RateIsPromotional);
