namespace Cardui.Api.Models;

/// <summary>
/// One immutable copy of a household's decision plan.
/// The document is the facts, assumptions, priorities, scenario, actions, milestones, and review conditions at save time.
/// Later edits to debts, pay, or bills do not change this row. Accepting a newer draft marks the previous accepted plan superseded.
/// </summary>
public class PlanVersion
{
    public const int CalculationVersionMaxLength = 32;

    public Guid Id { get; set; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public int Number { get; set; }

    public PlanVersionStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public DateTimeOffset? SupersededAt { get; set; }

    public required string CalculationVersion { get; set; }

    public DateOnly AsOf { get; set; }

    public required string PlanningCurrency { get; set; }

    /// <summary>
    /// The decision plan as JSON written by the plan rules.
    /// </summary>
    public required string Document { get; set; }
}
