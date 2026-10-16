namespace Cardui.Api.Domain.Planning;

/// <summary>
/// A planning input with its basis.
/// ConfirmedObligation is true only for a bill the person saved.
/// Amount is null for a date, a note, or a suggested account. AsOf is null when there is no date, including a monthly average.
/// Detail is the note text, the suggestion key, or the suggested account name.
/// </summary>
public sealed record PlanningFact(
    PlanningFactKind Kind,
    PlanningFactBasis Basis,
    PlanningFactUnit Unit,
    bool ConfirmedObligation,
    Guid? SubjectId,
    Guid? RelatedId,
    string Name,
    string? Detail,
    decimal? Amount,
    string? Currency,
    DateOnly? AsOf,
    string Explanation);
