using Cardui.Api.Domain.Obligations;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A proposed bill. Applied is false. The bill is not saved, and a suggestion is not confirmed.
/// </summary>
public sealed class BillFactProposalDto
{
    public bool Applied { get; init; }

    public required ObligationDraft Bill { get; init; }
}
