namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A proposed monthly contribution. Applied is false. Null clears the amount. The stored contribution stays as it is.
/// </summary>
public sealed class LivingContributionProposalDto
{
    public bool Applied { get; init; }

    public Guid ContributorId { get; init; }

    public decimal? MonthlyAmount { get; init; }
}
