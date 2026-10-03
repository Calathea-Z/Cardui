namespace Cardui.Api.Dtos.Household;

public class HouseholdDto
{
    public Guid Id { get; set; }

    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
