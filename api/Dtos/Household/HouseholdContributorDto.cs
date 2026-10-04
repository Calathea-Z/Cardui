namespace Cardui.Api.Dtos.Household;

public class HouseholdContributorDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public bool IsVisible { get; set; }
}
