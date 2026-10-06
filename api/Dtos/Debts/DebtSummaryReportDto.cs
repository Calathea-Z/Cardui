namespace Cardui.Api.Dtos.Debts;

public class DebtSummaryReportDto
{
    /// <summary>
    /// APR, as a percent, at which a rate is called out. 20 means 20 percent.
    /// </summary>
    public decimal AprNoticePercent { get; set; }

    /// <summary>
    /// Utilization ratio at which a revolving debt is called out. 0.30 means 30 percent.
    /// </summary>
    public decimal UtilizationNotice { get; set; }

    /// <summary>
    /// Utilization ratio at which a revolving debt is near its limit. 0.90 means 90 percent.
    /// </summary>
    public decimal UtilizationLimitNotice { get; set; }

    /// <summary>
    /// Days ahead, including today, in which a promotion end is called out.
    /// </summary>
    public int PromotionalNoticeDays { get; set; }

    public IReadOnlyList<DebtCurrencySummaryDto> Currencies { get; set; } = [];

    public IReadOnlyList<DebtSummaryItemDto> Debts { get; set; } = [];
}
