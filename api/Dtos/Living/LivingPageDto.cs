using Cardui.Api.Dtos.Savings;

namespace Cardui.Api.Dtos.Living;

/// <summary>
/// The Living page: who shares pay, monthly living spending, and the affordability gap.
/// </summary>
public sealed class LivingPageDto
{
    public required string PlanningCurrency { get; set; }

    public IReadOnlyList<LivingContributionDto> Contributions { get; set; } = [];

    /// <summary>
    /// Paychecks that name no person. They stay fully shared.
    /// </summary>
    public IReadOnlyList<string> UnassignedIncome { get; set; } = [];

    public SavingsGoalDto? LivingSpending { get; set; }

    public IReadOnlyList<SavingsAccountDto> Accounts { get; set; } = [];

    public required LivingGapDto Gap { get; set; }
}
