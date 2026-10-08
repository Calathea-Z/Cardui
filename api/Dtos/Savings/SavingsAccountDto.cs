namespace Cardui.Api.Dtos.Savings;

public sealed class SavingsAccountDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Mask { get; set; }

    public decimal Balance { get; set; }

    /// <summary>
    /// Blank when the account has no currency. A blank currency counts as the planning currency.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// The goal already following this account. Null when the account is free.
    /// </summary>
    public Guid? FollowedByGoalId { get; set; }
}
