namespace Cardui.Api.Domain.Allocation;

/// <summary>
/// Percents that can be stored. Each is a whole number from 0 to 100.
/// Debt and savings together stay at or under 100. The rest stays available.
/// </summary>
public sealed record AllocationPreferenceDraft(
    int VariableIncomeDebtPercent,
    int VariableIncomeSavingsPercent,
    int WindfallDebtPercent,
    int WindfallSavingsPercent);
