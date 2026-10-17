using Cardui.Api.Domain.Income;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A proposed income source. Applied is false. The source is not saved.
/// </summary>
public sealed class IncomeFactProposalDto
{
    public bool Applied { get; init; }

    public required IncomeSourceDraft Income { get; init; }
}
