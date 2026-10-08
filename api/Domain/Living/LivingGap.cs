namespace Cardui.Api.Domain.Living;

/// <summary>
/// Whether the shared monthly pay covers bills, minimums, and monthly living spending.
/// Shortfall is zero when the picture covers those amounts. LeftOut names amounts that are not in the monthly totals.
/// The figures are yearly averages. They are not cash on a date.
/// </summary>
public sealed record LivingGap(
    decimal SharedMonthly,
    decimal BillsMonthly,
    decimal MinimumsMonthly,
    decimal LivingSpendingMonthly,
    decimal Shortfall,
    IReadOnlyList<string> LeftOut);
