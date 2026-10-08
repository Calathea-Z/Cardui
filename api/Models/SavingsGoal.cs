namespace Cardui.Api.Models;

public class SavingsGoal
{
    public const int NameMaxLength = 80;

    public const string OperatingName = "Monthly living spending";

    public const string FloorName = "Cash to keep";

    public const string EmergencyName = "Emergency";

    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public SavingsGoalKind Kind { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The amount to have set aside by the target date.
    /// Null for monthly living spending and cash to keep, which do not finish on a date.
    /// </summary>
    public decimal? TargetAmount { get; set; }

    /// <summary>
    /// The date a named goal or the emergency goal is funded.
    /// Null for monthly living spending and cash to keep.
    /// </summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>
    /// How much living spending leaves cash each month. Null for every other kind.
    /// </summary>
    public decimal? MonthlyAmount { get; set; }

    /// <summary>
    /// The day of the month living spending counts. A short month uses its last day. Null for every other kind.
    /// </summary>
    public int? ReadyDay { get; set; }

    /// <summary>
    /// The cash floor the plan always protects. Null for every other kind.
    /// </summary>
    public decimal? FloorAmount { get; set; }

    /// <summary>
    /// The amount the person typed. Blank is stored as zero.
    /// While a follow has no override, the plan uses the account balance instead.
    /// </summary>
    public decimal ReservedAmount { get; set; }

    public required string Currency { get; set; }

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

    /// <summary>
    /// Set while the goal follows an account. Null means the reserved amount is only what was typed.
    /// </summary>
    public DateTimeOffset? AccountFollowedSince { get; set; }

    /// <summary>
    /// Set when the person keeps their own amount while an account is followed.
    /// Null means the plan uses the account balance.
    /// </summary>
    public DateTimeOffset? ReservedOverriddenAt { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
