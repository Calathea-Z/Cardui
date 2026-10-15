namespace Cardui.Api.Dtos.Allocation;

/// <summary>
/// One saved split. CashPercent is what stays available after debt and savings.
/// </summary>
public sealed class AllocationRuleDto
{
    public int DebtPercent { get; set; }

    public int SavingsPercent { get; set; }

    public int CashPercent { get; set; }
}
