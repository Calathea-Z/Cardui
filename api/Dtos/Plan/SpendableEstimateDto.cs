namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Spendable cash for one payoff path over the 30-day horizon.
/// Spendable is never negative. LowestAvailable stays negative when the horizon is already short.
/// The income, obligation, and spending totals are dated amounts inside the horizon, not a monthly surplus.
/// </summary>
public class SpendableEstimateDto
{
    public DateOnly From { get; set; }

    public DateOnly Through { get; set; }

    public decimal Spendable { get; set; }

    public decimal LowestAvailable { get; set; }

    public DateOnly LowestOn { get; set; }

    public decimal ReliableIncome { get; set; }

    public decimal Obligations { get; set; }

    public decimal OrdinarySpending { get; set; }

    public decimal Reserved { get; set; }

    public bool AlreadyShort { get; set; }

    public required SpendableGuardrailDto Guardrail { get; set; }

    public IReadOnlyList<SpendableUncertaintyDto> Uncertainties { get; set; } = [];
}
