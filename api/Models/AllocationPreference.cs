namespace Cardui.Api.Models;

/// <summary>
/// One household's saved split for extra variable pay and a windfall.
/// Each pair of percents is a whole number from 0 to 100, and together they
/// cannot pass 100. The remainder stays available. Saving this does not move money.
/// </summary>
public class AllocationPreference
{
    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public int VariableIncomeDebtPercent { get; set; }

    public int VariableIncomeSavingsPercent { get; set; }

    public int WindfallDebtPercent { get; set; }

    public int WindfallSavingsPercent { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
