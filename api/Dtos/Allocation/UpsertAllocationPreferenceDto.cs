namespace Cardui.Api.Dtos.Allocation;

public sealed class UpsertAllocationPreferenceDto
{
    public int VariableIncomeDebtPercent { get; set; }

    public int VariableIncomeSavingsPercent { get; set; }

    public int WindfallDebtPercent { get; set; }

    public int WindfallSavingsPercent { get; set; }
}
