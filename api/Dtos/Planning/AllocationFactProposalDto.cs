using Cardui.Api.Domain.Allocation;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// A proposed allocation. Applied is false. The percents are not saved, and no money is moved.
/// </summary>
public sealed class AllocationFactProposalDto
{
    public bool Applied { get; init; }

    public required AllocationPreferenceDraft Allocation { get; init; }
}
