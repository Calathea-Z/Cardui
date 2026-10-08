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
