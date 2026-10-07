namespace Cardui.Api.Dtos.Debts;

public class DebtCurrencySummaryDto
{
    public required string Currency { get; set; }

    public int DebtCount { get; set; }

    /// <summary>
    /// Sum of recorded balances in this currency. Null when every balance is unknown.
    /// </summary>
    public decimal? RecordedBalance { get; set; }

    public int UnknownBalanceCount { get; set; }

    /// <summary>
    /// Sum of the monthly interest estimates. Null when every estimate is unknown.
    /// </summary>
    public decimal? MonthlyInterest { get; set; }

    public int UnknownInterestCount { get; set; }

    /// <summary>
    /// Sum of known minimums. Null when every minimum is unknown.
    /// </summary>
    public decimal? MinimumPayments { get; set; }

    public int UnknownMinimumCount { get; set; }

    /// <summary>
    /// Share of the known revolving limits in use, as a ratio. Null when none can be calculated.
    /// </summary>
    public decimal? Utilization { get; set; }

    public int UtilizationDebtCount { get; set; }

    public int UnknownUtilizationCount { get; set; }

    public int DueDatePassedCount { get; set; }

    public int PromoEndingCount { get; set; }

    public int PromotionEndedCount { get; set; }

    public int AprNoticeCount { get; set; }

    public int UtilizationNoticeCount { get; set; }

    public int UtilizationLimitNoticeCount { get; set; }

    public int BalanceDifferenceCount { get; set; }

    public int MissingDueDateCount { get; set; }

    public int MissingRemainingTermCount { get; set; }

    public int MissingPromotionalEndCount { get; set; }

    public int MissingPromotionalRateCount { get; set; }

    public int MissingRateAfterPromotionCount { get; set; }

    /// <summary>
    /// Followed debts whose connection is not current. Their balance is still included in the totals.
    /// </summary>
    public int StaleCount { get; set; }
}
