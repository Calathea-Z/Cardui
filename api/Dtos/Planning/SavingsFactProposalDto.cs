using Cardui.Api.Domain.Savings;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A proposed savings row. Applied is false. The goal is not saved, and no account balance is copied.
/// </summary>
public sealed class SavingsFactProposalDto
{
    public bool Applied { get; init; }

    public required SavingsGoalDraft Savings { get; init; }
}
