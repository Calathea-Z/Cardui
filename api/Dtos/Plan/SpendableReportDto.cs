namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Spendable cash on the rollover path and on the path that keeps freed payments.
/// </summary>
public class SpendableReportDto
{
    public required SpendableEstimateDto Rollover { get; set; }

    public required SpendableEstimateDto ReclaimAll { get; set; }
}
