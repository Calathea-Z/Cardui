namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One amount on one date.
/// Amount is positive. The kind says whether cash comes in or goes out.
/// A biweekly amount stays on its dates. It is not rewritten as a monthly average.
/// </summary>
public sealed record CashFlowEvent(
    DateOnly Date,
    CashFlowKind Kind,
    Guid SourceId,
    string Name,
    decimal Amount,
    string Currency);
