using Cardui.Api.Domain.Planning;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// One labeled planning fact for the signed-in household.
/// ConfirmedObligation is true only for a bill the person saved. A suggestion stays false.
/// Amount is null for a date, a note, or a suggested account. AsOf is null when the fact has no date.
/// A monthly average never has a date. Detail is words, and it is not a saved amount.
/// </summary>
public sealed class PlanningFactDto
{
    public PlanningFactKind Kind { get; init; }

    public PlanningFactBasis Basis { get; init; }

    public PlanningFactUnit Unit { get; init; }

    public bool ConfirmedObligation { get; init; }

    public Guid? SubjectId { get; init; }

    public Guid? RelatedId { get; init; }

    public required string Name { get; init; }

    public string? Detail { get; init; }

    public decimal? Amount { get; init; }

    public string? Currency { get; init; }

    public DateOnly? AsOf { get; init; }

    public required string Explanation { get; init; }
}
