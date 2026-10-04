namespace Cardui.Api.Dtos.Household;

public class FinancialProfileDto
{
    public required string PlanningCurrency { get; set; }

    public required string TimeZoneId { get; set; }

    public IReadOnlyList<HouseholdContributorDto> Contributors { get; set; } = [];
}
