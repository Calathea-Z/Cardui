namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Where a planning fact came from.
/// A user statement is something the person entered or saved.
/// An imported fact came from a connected account or a file.
/// An estimate is a derived figure, and it is not cash on a date.
/// An unverified suggestion has not been confirmed. It is not a bill, and it is not followed.
/// The member name is the JSON text. This value is not stored.
/// </summary>
public enum PlanningFactBasis
{
    UserStatement,
    ImportedFact,
    Estimate,
    UnverifiedSuggestion
}
