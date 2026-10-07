using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Dtos.Debts;

/// <summary>
/// One plain reason an account was suggested for a debt.
/// Mask is set for the last digits. Words are set for a shared name. Difference is the absolute balance gap.
/// A difference of zero means the amounts are the same. The other fields are empty for that reason.
/// </summary>
public sealed class DebtMatchReasonDto
{
    public DebtMatchReasonKind Kind { get; set; }

    public string? Mask { get; set; }

    public IReadOnlyList<string> Words { get; set; } = [];

    public decimal? Difference { get; set; }
}
