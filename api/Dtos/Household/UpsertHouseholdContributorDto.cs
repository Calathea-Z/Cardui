namespace Cardui.Api.Dtos.Household;

public class UpsertHouseholdContributorDto
{
    public required string Name { get; set; }

    public bool IsVisible { get; set; } = true;
}
