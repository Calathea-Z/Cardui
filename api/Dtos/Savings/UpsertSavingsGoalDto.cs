using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Savings;

public sealed class UpsertSavingsGoalDto
{
    public SavingsGoalKind? Kind { get; set; }

    public string? Name { get; set; }

    /// <summary>
    /// The amount to reach by the target date. Null for everyday spending and cash to keep.
    /// </summary>
    public decimal? TargetAmount { get; set; }

    /// <summary>
    /// The date a finishing goal is funded. Null for everyday spending and cash to keep.
    /// </summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>
    /// How much everyday spending leaves cash each month. Null for every other kind.
    /// </summary>
    public decimal? MonthlyAmount { get; set; }

    /// <summary>
    /// The day of the month everyday spending counts, from 1 to 31. Null for every other kind.
    /// </summary>
    public int? ReadyDay { get; set; }

    /// <summary>
    /// The cash floor to always protect. Null for every other kind.
    /// </summary>
    public decimal? FloorAmount { get; set; }

    /// <summary>
    /// Available now for everyday spending and cash to keep, or the amount set aside on a finishing goal.
    /// Zero means nothing was typed.
    /// </summary>
    public decimal ReservedAmount { get; set; }

    public Guid? AccountId { get; set; }

    /// <summary>
    /// When true, a followed account's balance replaces the typed amount.
    /// </summary>
    public bool UseAccountBalance { get; set; }
}
