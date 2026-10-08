namespace Cardui.Api.Dtos.Living;

/// <summary>
/// The current monthly benchmark used to derive one person's share of scheduled pay.
/// Null clears it so all recorded pay stays shared.
/// </summary>
public sealed class UpsertLivingContributionDto
{
    public decimal? MonthlyAmount { get; set; }
}
