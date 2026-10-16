namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Spendable cash on keeping none, some, or all of a freed payment.
/// </summary>
public class SpendableReportDto
{
    public required SpendableEstimateDto Rollover { get; set; }

    public required SpendableEstimateDto Reclaim { get; set; }

    public required SpendableEstimateDto ReclaimAll { get; set; }
}
