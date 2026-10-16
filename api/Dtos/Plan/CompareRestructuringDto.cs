using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// Terms for one restructuring comparison. Nothing is saved.
/// A blank field stays null so the comparison can ask for it.
/// </summary>
public sealed class CompareRestructuringDto
{
    public RestructuringOfferKind? Kind { get; set; }

    public List<Guid>? DebtIds { get; set; }

    public decimal? Apr { get; set; }

    public int? TermMonths { get; set; }

    public decimal? Fee { get; set; }

    public RestructuringFeeFunding? FeeFunding { get; set; }

    public decimal? UpfrontCash { get; set; }
}
