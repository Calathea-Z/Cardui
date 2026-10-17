using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A proposed debt. Applied is false. The debt is not saved.
/// </summary>
public sealed class DebtFactProposalDto
{
    public bool Applied { get; init; }

    public required DebtDraft Debt { get; init; }
}
