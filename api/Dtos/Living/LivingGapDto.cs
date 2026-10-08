namespace Cardui.Api.Dtos.Living;

/// <summary>
/// The monthly picture of shared pay against bills, minimums, and monthly living spending.
/// Shortfall is zero when those amounts are covered. LeftOut names amounts that are not in the totals.
/// The figures are averages. They are not cash on a date.
/// </summary>
public sealed class LivingGapDto
{
    public decimal SharedMonthly { get; set; }

    public decimal BillsMonthly { get; set; }

    public decimal MinimumsMonthly { get; set; }

    public decimal LivingSpendingMonthly { get; set; }

    public decimal Shortfall { get; set; }

    public IReadOnlyList<string> LeftOut { get; set; } = [];
}
