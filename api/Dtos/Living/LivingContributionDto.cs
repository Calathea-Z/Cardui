using Cardui.Api.Domain.Living;

namespace Cardui.Api.Dtos.Living;

/// <summary>
/// One person's current contribution benchmark and how it compares with recorded pay.
/// MonthlyAmount is null when all pay is shared. RecordedMonthly is null when they have no scheduled pay in the planning currency.
/// </summary>
public sealed class LivingContributionDto
{
    public Guid ContributorId { get; set; }

    public required string Name { get; set; }

    public decimal? MonthlyAmount { get; set; }

    public decimal? RecordedMonthly { get; set; }

    public decimal SharedMonthly { get; set; }

    public decimal KeptMonthly { get; set; }

    public ContributionLimit Limit { get; set; }

    /// <summary>
    /// True when a paycheck has no schedule and still arrives on its own date.
    /// </summary>
    public bool HasUnscheduledPay { get; set; }
}
