namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One offer compared with keeping the selected debts on their current minimums.
/// MonthlyCashFlow is those minimums minus the offer payment. Positive means more cash left each month.
/// That difference is not a rank. A lower payment is not treated as the better offer.
/// Keep and offer totals are interest, and the offer total also includes the fee. Null when that path does not pay off.
/// Upfront cash is reported apart from those totals. The offer is not treated as approved.
/// </summary>
public sealed record RestructuringComparison(
    RestructuringOfferKind Kind,
    IReadOnlyList<RestructuringDebtOutcome> Debts,
    decimal CoveredBalance,
    decimal Fee,
    RestructuringFeeFunding FeeFunding,
    decimal UpfrontCash,
    decimal CashDueNow,
    decimal NewPrincipal,
    decimal OfferApr,
    int TermMonths,
    int? OfferPaymentCount,
    decimal OfferMonthlyPayment,
    decimal? KeepMonthlyPayment,
    decimal? MonthlyCashFlow,
    decimal? KeepTotalInterest,
    decimal? OfferTotalInterest,
    decimal? KeepTotalCost,
    decimal? OfferTotalCost,
    DateOnly? KeepPaidOffOn,
    DateOnly? OfferPaidOffOn,
    DebtScheduleStop OfferStop,
    RestructuringBreakEven BreakEven,
    DateOnly? BreakEvenOn,
    RestructuringCashEffect Cash);
