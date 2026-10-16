namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Checks a planning tool before it runs.
/// A tool may read a summary, propose a fact, calculate, compare, or draft. It does not save.
/// </summary>
public static class PlanningToolRules
{
    /// <summary>
    /// Rejects a summary this tool cannot read.
    /// </summary>
    public static string? RejectSummary(PlanningSummaryKind kind)
    {
        return Enum.IsDefined(kind) ? null : "Choose a summary.";
    }

    /// <summary>
    /// Rejects a suggestion key on a proposed bill.
    /// A suggestion stays a suggestion until the person saves the bill. A blank key is a hand-entered bill.
    /// </summary>
    public static string? RejectSuggestion(string? suggestionKey)
    {
        return string.IsNullOrWhiteSpace(suggestionKey)
            ? null
            : "A suggestion is not a bill until it is saved.";
    }
}
