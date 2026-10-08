namespace Cardui.Api.Dtos.Plan;

public class PlanCashOutlookDto
{
    /// <summary>
    /// The first day of the outlook, today in the household time zone.
    /// </summary>
    public DateOnly AsOf { get; set; }

    /// <summary>
    /// The Cash total on Accounts the outlook starts from, before today's payments.
    /// </summary>
    public decimal StartingCash { get; set; }

    /// <summary>
    /// The amount already set aside at the start of the outlook. Zero when nothing is reserved.
    /// </summary>
    public decimal StartingReserve { get; set; }

    /// <summary>
    /// Cash minus the reserve at the start. Negative when more is set aside than the cash on hand.
    /// </summary>
    public decimal StartingAvailable { get; set; }

    /// <summary>
    /// True when at least one income source counts in the planning currency.
    /// </summary>
    public bool HasIncome { get; set; }

    /// <summary>
    /// True when at least one bill counts in the planning currency.
    /// </summary>
    public bool HasBills { get; set; }

    /// <summary>
    /// Income, bill, and debt currency codes left out of the outlook.
    /// </summary>
    public IReadOnlyList<string> ExcludedCurrencies { get; set; } = [];

    public required PlanCashOutlookPathDto Rollover { get; set; }

    public required PlanCashOutlookPathDto ReclaimAll { get; set; }
}
