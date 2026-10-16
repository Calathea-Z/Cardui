using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Services.Interfaces;

public interface IRestructuringService
{
    /// <summary>
    /// Compares one consolidation loan, balance transfer, or replacement loan with the selected debts.
    /// The debts are included in full at their current minimums. The offer rate stays fixed for the term entered.
    /// Cash due now is compared with cash on hand, the reserve, and cash to keep. It is not an investment return.
    /// A lower payment is not ranked as better. Nothing is saved, and the offer is not treated as approved.
    /// </summary>
    Task<RestructuringComparisonDto> CompareAsync(
        CompareRestructuringDto dto,
        CancellationToken cancellationToken = default);
}
