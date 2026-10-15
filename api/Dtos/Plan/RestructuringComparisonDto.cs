using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One offer compared with keeping the selected debts on their current minimums.
/// MonthlyCashFlow is those minimums minus the offer payment. Positive means more cash left each month.
/// It is not a rank. Totals are null when that path does not pay off. The offer is not treated as approved.
/// </summary>
public sealed class RestructuringComparisonDto
{
    public RestructuringOfferKind Kind { get; set; }

    public List<RestructuringDebtOutcomeDto> Debts { get; set; } = [];

    public decimal CoveredBalance { get; set; }

    public decimal Fee { get; set; }

    public RestructuringFeeFunding FeeFunding { get; set; }

    public decimal UpfrontCash { get; set; }

    /// <summary>
    /// A fee paid in cash plus upfront cash. A fee added to the balance is not included.
    /// </summary>
    public decimal CashDueNow { get; set; }

    public decimal NewPrincipal { get; set; }

    public decimal OfferApr { get; set; }

    public int TermMonths { get; set; }

    /// <summary>
    /// Payments until the offer is paid off. Null when the offer does not pay off.
    /// Rounding can make this one month longer than TermMonths.
    /// </summary>
    public int? OfferPaymentCount { get; set; }

    public decimal OfferMonthlyPayment { get; set; }

    public decimal? KeepMonthlyPayment { get; set; }

    public decimal? MonthlyCashFlow { get; set; }

    public decimal? KeepTotalInterest { get; set; }

    public decimal? OfferTotalInterest { get; set; }

    public decimal? KeepTotalCost { get; set; }

    public decimal? OfferTotalCost { get; set; }

    public DateOnly? KeepPaidOffOn { get; set; }

    public DateOnly? OfferPaidOffOn { get; set; }

    public DebtScheduleStop OfferStop { get; set; }

    public RestructuringBreakEven BreakEven { get; set; }

    public DateOnly? BreakEvenOn { get; set; }

    public RestructuringCashEffectDto Cash { get; set; } = new();
}
