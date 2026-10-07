namespace Cardui.Api.Domain.Debts;

/// <summary>
/// Inventory summary for the household's debts.
/// The notice numbers are the thresholds the view names. They are not combined into a score.
/// </summary>
public sealed record DebtSummaryReport(
    decimal AprNoticePercent,
    decimal UtilizationNotice,
    decimal UtilizationLimitNotice,
    int PromotionalNoticeDays,
    IReadOnlyList<DebtCurrencySummary> Currencies,
    IReadOnlyList<DebtSummaryItem> Debts);
