using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One reason the spendable estimate leaves something out.
/// Name is empty when the reason has no income, debt, or currency.
/// </summary>
public class SpendableUncertaintyDto
{
    public SpendableUncertaintyKind Kind { get; set; }

    public string Name { get; set; } = "";
}
