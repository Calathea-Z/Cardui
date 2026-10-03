namespace Cardui.Api.Models;

public class Household
{
    public const int OwnerClerkUserIdMaxLength = 128;
    public const int DisplayNameMaxLength = 200;

    public Guid Id { get; init; }

    public required string OwnerClerkUserId { get; init; }

    public required string DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
