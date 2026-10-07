namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The order produced by pay-first and pay-last constraints, plus the notes used to explain it.
/// Notes follow the constraints that were read. A repeated debt is noted once.
/// </summary>
internal sealed record PayoffConstraintBuild(
    IReadOnlyList<PayoffDebt> Order,
    IReadOnlyList<PayoffConstraintNote> Notes);
